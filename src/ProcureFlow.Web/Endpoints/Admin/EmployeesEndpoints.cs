using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Admin;

public static class EmployeesEndpoints
{
    public static RouteGroupBuilder MapEmployeesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/companies/{companyId:int}/employees", CreateEmployeeAsync);
        group.MapGet("/companies/{companyId:int}/employees", ListEmployeesAsync);
        group.MapGet("/employees/{employeeId:int}", GetEmployeeAsync);
        group.MapPatch("/employees/{employeeId:int}", UpdateEmployeeAsync);
        return group;
    }

    private static async Task<IResult> CreateEmployeeAsync(
        [FromRoute] int companyId,
        [FromBody] CreateEmployeeRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var companyExists = await dbContext.Companies.AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return Results.NotFound(new { code = "COMPANY_NOT_FOUND" });
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Invalid employee status" }
            });
        }

        var duplicateEmployeeCode = await dbContext.CompanyEmployees
            .AnyAsync(x => x.CompanyId == companyId && x.EmployeeCode == request.EmployeeCode, cancellationToken);
        if (duplicateEmployeeCode)
        {
            return Results.Conflict(new { code = "EMPLOYEE_CODE_CONFLICT" });
        }

        var actor = httpContext.User.Identity?.Name ?? "system";
        var employee = new CompanyEmployee(companyId, request.EmployeeCode.Trim(), request.FullName.Trim(), request.Email.Trim())
        {
            Phone = request.Phone.Trim(),
            Status = request.Status,
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.CompanyEmployees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/employees/{employee.Id}", new EmployeeResponse(employee));
    }

    private static async Task<IResult> ListEmployeesAsync(
        [FromRoute] int companyId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.CompanyEmployees.Where(x => x.CompanyId == companyId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeResponse(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(new EmployeeListResponse(total, page, pageSize, items));
    }

    private static async Task<IResult> GetEmployeeAsync(
        [FromRoute] int employeeId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.CompanyEmployees.FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new EmployeeResponse(employee));
    }

    private static async Task<IResult> UpdateEmployeeAsync(
        [FromRoute] int employeeId,
        [FromBody] UpdateEmployeeRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Invalid employee status" }
            });
        }

        var employee = await dbContext.CompanyEmployees.FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Results.NotFound();
        }

        if (employee.UpdatedAtUtc != request.UpdatedAtUtc)
        {
            return Results.Conflict(new { code = "OPTIMISTIC_CONCURRENCY_CONFLICT" });
        }

        employee.FullName = request.FullName.Trim();
        employee.Email = request.Email.Trim();
        employee.Phone = request.Phone.Trim();
        employee.Status = request.Status;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        employee.UpdatedBy = httpContext.User.Identity?.Name ?? "system";

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new EmployeeResponse(employee));
    }
}

public record CreateEmployeeRequest(string EmployeeCode, string FullName, string Email, string Phone, EmployeeStatus Status);

public record UpdateEmployeeRequest(string FullName, string Email, string Phone, EmployeeStatus Status, DateTime UpdatedAtUtc);

public record EmployeeResponse(int Id, int CompanyId, string EmployeeCode, string FullName, string Email, EmployeeStatus Status, DateTime UpdatedAtUtc)
{
    public EmployeeResponse(CompanyEmployee employee)
        : this(employee.Id, employee.CompanyId, employee.EmployeeCode, employee.FullName, employee.Email, employee.Status, employee.UpdatedAtUtc)
    {
    }
}

public record EmployeeListResponse(int Total, int Page, int PageSize, IReadOnlyCollection<EmployeeResponse> Items);