using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Vendor;

public static class VendorInviteEndpoints
{
    public static RouteGroupBuilder MapVendorPortalInviteEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/invites", ListVendorInvitesAsync);
        return group;
    }

    private static async Task<IResult> ListVendorInvitesAsync(
        [FromQuery] int? companyId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!companyId.HasValue || companyId.Value <= 0)
        {
            return Results.BadRequest(new { code = "COMPANY_ID_REQUIRED", message = "companyId query parameter is required." });
        }

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.RfpVendorParticipations.AsNoTracking()
            .Include(v => v.Rfp)
            .ThenInclude(r => r.Company)
            .AsQueryable();

        query = query.Where(v => v.CompanyId == companyId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.InviteAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorInviteListItem(
                v.Id,
                v.RfpId,
                v.CompanyId,
                (int)v.Status,
                v.InviteAt,
                v.ResponseAt,
                v.Rfp.Title,
                v.Rfp.Company.LegalName,
                v.Rfp.Deadline))
            .ToListAsync(cancellationToken);

        return Results.Ok(new VendorInviteListResponse(total, page, pageSize, items));
    }
}

public sealed record VendorInviteListItem(
    int Id,
    int RfpId,
    int CompanyId,
    int Status,
    DateTime InviteAt,
    DateTime? ResponseAt,
    string RfpTitle,
    string BuyerCompanyName,
    DateTime? Deadline);

public sealed record VendorInviteListResponse(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyCollection<VendorInviteListItem> Items);
