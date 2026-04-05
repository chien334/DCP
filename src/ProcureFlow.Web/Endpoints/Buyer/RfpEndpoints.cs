using System.Security.Claims;
using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Buyer;

public static class RfpEndpoints
{
    public static RouteGroupBuilder MapRfpEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rfps", CreateRfpAsync);
        group.MapGet("/rfps", ListRfpsAsync);
        group.MapGet("/rfps/{rfpId:int}", GetRfpDetailAsync);
        return group;
    }

    // ── POST /api/buyer/rfps ─────────────────────────────────────────────────────

    private static async Task<IResult> CreateRfpAsync(
        [FromBody] CreateRfpRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["title"] = ["Title is required"] });

        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists)
            return Results.NotFound(new { code = "COMPANY_NOT_FOUND" });

        var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
            return Results.UnprocessableEntity(new { code = "CATEGORY_NOT_FOUND" });

        var rfp = new Rfp
        {
            CompanyId = request.CompanyId,
            Title = request.Title.Trim(),
            Description = (request.Description ?? string.Empty).Trim(),
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            CategoryId = request.CategoryId,
            Note = request.Note?.Trim(),
            Type = request.Type,
            PrivacyMode = request.PrivacyMode,
            Status = RfpStatus.Draft,
            Deadline = request.Deadline,
        };

        if (request.Items is { Count: > 0 })
        {
            foreach (var itemReq in request.Items)
            {
                var item = new RfpItem
                {
                    Name = itemReq.Name.Trim(),
                    Quantity = itemReq.Quantity,
                    Unit = itemReq.Unit.Trim(),
                    Note = itemReq.Note?.Trim(),
                    Status = RfpItemStatus.Active,
                };

                if (itemReq.Specs is { Count: > 0 })
                {
                    foreach (var specReq in itemReq.Specs)
                    {
                        item.Specs.Add(new RfpItemSpec
                        {
                            Key = specReq.Key.Trim(),
                            ValueText = specReq.ValueText?.Trim(),
                            ValueNumber = specReq.ValueNumber,
                            ValueBoolean = specReq.ValueBoolean,
                            Unit = specReq.Unit?.Trim(),
                            Status = RfpItemSpecStatus.Active,
                        });
                    }
                }

                rfp.Items.Add(item);
            }
        }

        if (request.Attachments is { Count: > 0 })
        {
            foreach (var attReq in request.Attachments)
            {
                rfp.Attachments.Add(new RfpAttachment
                {
                    FileName = attReq.FileName.Trim(),
                    FileUrl = attReq.FileUrl.Trim(),
                    Status = RfpAttachmentStatus.Active,
                });
            }
        }

        dbContext.Rfps.Add(rfp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/buyer/rfps/{rfp.Id}", new { rfp.Id });
    }

    // ── GET /api/buyer/rfps ──────────────────────────────────────────────────────

    private static async Task<IResult> ListRfpsAsync(
        [FromQuery] int? companyId,
        [FromQuery] int? status,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.Rfps.AsNoTracking().AsQueryable();

        if (companyId.HasValue)
            query = query.Where(r => r.CompanyId == companyId.Value);

        if (status.HasValue)
            query = query.Where(r => (int)r.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RfpListItem(
                r.Id, r.CompanyId, r.Title, r.BudgetMin, r.BudgetMax,
                (int)r.Status, r.Deadline, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(new RfpListResponse(total, page, pageSize, items));
    }

    // ── GET /api/buyer/rfps/{rfpId} ──────────────────────────────────────────────

    private static async Task<IResult> GetRfpDetailAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfp = await dbContext.Rfps.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Specs)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == rfpId, cancellationToken);

        if (rfp is null)
            return Results.NotFound();

        var detail = new RfpDetailResponse(
            rfp.Id, rfp.CompanyId, rfp.Title, rfp.Description,
            rfp.BudgetMin, rfp.BudgetMax, rfp.CategoryId, rfp.Note,
            (int)rfp.Type, (int)rfp.PrivacyMode, (int)rfp.Status,
            rfp.Deadline, rfp.CreatedAtUtc, rfp.CreatedBy, rfp.UpdatedAtUtc, rfp.UpdatedBy,
            rfp.Items.Select(i => new RfpItemDto(
                i.Id, i.Name, i.Quantity, i.Unit, i.Note, (int)i.Status,
                i.Specs.Select(s => new RfpItemSpecDto(
                    s.Id, s.Key, s.ValueText, s.ValueNumber, s.ValueBoolean, s.Unit, (int)s.Status
                )).ToList()
            )).ToList(),
            rfp.Attachments.Select(a => new RfpAttachmentDto(
                a.Id, a.FileName, a.FileUrl, (int)a.Status
            )).ToList());

        return Results.Ok(detail);
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateRfpRequest(
    int CompanyId, string Title, string? Description,
    decimal? BudgetMin, decimal? BudgetMax,
    int CategoryId, string? Note,
    RfpType Type, RfpPrivacyMode PrivacyMode,
    DateTime? Deadline,
    List<CreateRfpItemRequest>? Items,
    List<CreateRfpAttachmentRequest>? Attachments);

public sealed record CreateRfpItemRequest(
    string Name, int Quantity, string Unit, string? Note,
    List<CreateRfpItemSpecRequest>? Specs);

public sealed record CreateRfpItemSpecRequest(
    string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean, string? Unit);

public sealed record CreateRfpAttachmentRequest(string FileName, string FileUrl);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record RfpListItem(
    int Id, int CompanyId, string Title,
    decimal? BudgetMin, decimal? BudgetMax,
    int Status, DateTime? Deadline, DateTime CreatedAtUtc);

public sealed record RfpListResponse(
    int Total, int Page, int PageSize, IReadOnlyCollection<RfpListItem> Items);

public sealed record RfpDetailResponse(
    int Id, int CompanyId, string Title, string Description,
    decimal? BudgetMin, decimal? BudgetMax, int CategoryId,
    string? Note, int Type, int PrivacyMode, int Status,
    DateTime? Deadline, DateTime CreatedAtUtc, string CreatedBy,
    DateTime UpdatedAtUtc, string UpdatedBy,
    IReadOnlyCollection<RfpItemDto> Items,
    IReadOnlyCollection<RfpAttachmentDto> Attachments);

public sealed record RfpItemDto(
    int Id, string Name, int Quantity, string Unit, string? Note, int Status,
    IReadOnlyCollection<RfpItemSpecDto> Specs);

public sealed record RfpItemSpecDto(
    int Id, string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean,
    string? Unit, int Status);

public sealed record RfpAttachmentDto(int Id, string FileName, string FileUrl, int Status);
