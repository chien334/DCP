using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Admin;

public static class CompaniesEndpoints
{
    public static RouteGroupBuilder MapCompaniesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/companies", CreateCompanyAsync);
        group.MapPatch("/companies/{companyId:int}", UpdateCompanyAsync);
        return group;
    }

    private static async Task<IResult> CreateCompanyAsync(
        [FromBody] CreateCompanyRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LegalName) || string.IsNullOrWhiteSpace(request.TaxCode))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["legalName"] = new[] { "LegalName is required" },
                ["taxCode"] = new[] { "TaxCode is required" }
            });
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Invalid company status" }
            });
        }

        var duplicated = await dbContext.Companies.AnyAsync(x => x.TaxCode == request.TaxCode, cancellationToken);
        if (duplicated)
        {
            return Results.Conflict(new { code = "COMPANY_TAX_CODE_CONFLICT" });
        }

        var actor = httpContext.User.Identity?.Name ?? "system";
        var now = DateTime.UtcNow;

        var company = new Company
        {
            LegalName = request.LegalName.Trim(),
            ShortName = request.ShortName.Trim(),
            TaxCode = request.TaxCode.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Status = request.Status,
            CreatedAtUtc = now,
            CreatedBy = actor,
            UpdatedAtUtc = now,
            UpdatedBy = actor,
            Address = new CompanyAddress
            {
                Country = request.Address.Country.Trim(),
                Province = request.Address.Province.Trim(),
                District = request.Address.District.Trim(),
                Ward = request.Address.Ward.Trim(),
                AddressLine = request.Address.AddressLine.Trim(),
                PostalCode = request.Address.PostalCode.Trim()
            }
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/companies/{company.Id}", new CompanyResponse(company));
    }

    private static async Task<IResult> UpdateCompanyAsync(
        [FromRoute] int companyId,
        [FromBody] UpdateCompanyRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Invalid company status" }
            });
        }

        var company = await dbContext.Companies.Include(x => x.Address)
            .FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken);
        if (company is null)
        {
            return Results.NotFound();
        }

        if (company.UpdatedAtUtc != request.UpdatedAtUtc)
        {
            return Results.Conflict(new { code = "OPTIMISTIC_CONCURRENCY_CONFLICT" });
        }

        company.LegalName = request.LegalName.Trim();
        company.ShortName = request.ShortName.Trim();
        company.Email = request.Email.Trim();
        company.Phone = request.Phone.Trim();
        company.Status = request.Status;
        company.UpdatedAtUtc = DateTime.UtcNow;
        company.UpdatedBy = httpContext.User.Identity?.Name ?? "system";

        if (company.Address is not null)
        {
            company.Address.Country = request.Address.Country.Trim();
            company.Address.Province = request.Address.Province.Trim();
            company.Address.District = request.Address.District.Trim();
            company.Address.Ward = request.Address.Ward.Trim();
            company.Address.AddressLine = request.Address.AddressLine.Trim();
            company.Address.PostalCode = request.Address.PostalCode.Trim();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new CompanyResponse(company));
    }
}

public record AddressRequest(
    string Country,
    string Province,
    string District,
    string Ward,
    string AddressLine,
    string PostalCode);

public record CreateCompanyRequest(
    string LegalName,
    string ShortName,
    string TaxCode,
    string Email,
    string Phone,
    CompanyStatus Status,
    AddressRequest Address);

public record UpdateCompanyRequest(
    string LegalName,
    string ShortName,
    string Email,
    string Phone,
    CompanyStatus Status,
    AddressRequest Address,
    DateTime UpdatedAtUtc);

public record CompanyResponse(int Id, string LegalName, string TaxCode, CompanyStatus Status, DateTime UpdatedAtUtc)
{
    public CompanyResponse(Company company)
        : this(company.Id, company.LegalName, company.TaxCode, company.Status, company.UpdatedAtUtc)
    {
    }
}