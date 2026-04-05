using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Vendor;

public static class BidEndpoints
{
    public static RouteGroupBuilder MapBidEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/bids", CreateBidAsync);
        group.MapGet("/bids", ListMyBidsAsync);
        group.MapGet("/bids/{bidId:int}", GetBidDetailAsync);
        return group;
    }

    // ── POST /api/vendor/bids ─────────────────────────────────────────────────────

    private static async Task<IResult> CreateBidAsync(
        [FromBody] CreateBidRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Verify RFP exists
        var rfpExists = await dbContext.Rfps.AnyAsync(r => r.Id == request.RfpId, cancellationToken);
        if (!rfpExists)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        // Verify vendor company exists
        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists)
            return Results.NotFound(new { code = "COMPANY_NOT_FOUND" });

        // Verify vendor was invited to this RFP
        var participation = await dbContext.RfpVendorParticipations
            .FirstOrDefaultAsync(v => v.RfpId == request.RfpId && v.CompanyId == request.CompanyId, cancellationToken);
        if (participation is null)
            return Results.UnprocessableEntity(new { code = "NOT_INVITED" });

        // Check no existing bid from this vendor for this RFP
        var existingBid = await dbContext.RfpBids
            .AnyAsync(b => b.RfpId == request.RfpId && b.CompanyId == request.CompanyId, cancellationToken);
        if (existingBid)
            return Results.Conflict(new { code = "BID_ALREADY_EXISTS" });

        // Validate RFP items exist
        if (request.Items is { Count: > 0 })
        {
            var rfpItemIds = request.Items.Select(i => i.RfpItemId).Distinct().ToList();
            var validItemIds = await dbContext.RfpItems
                .Where(i => i.RfpId == request.RfpId && rfpItemIds.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            var invalidIds = rfpItemIds.Except(validItemIds).ToList();
            if (invalidIds.Count > 0)
                return Results.UnprocessableEntity(new { code = "INVALID_RFP_ITEMS", invalidIds });
        }

        var bid = new RfpBid
        {
            RfpId = request.RfpId,
            CompanyId = request.CompanyId,
            VatRate = request.VatRate,
            SubTotal = request.SubTotal,
            GrandTotal = request.GrandTotal,
            Currency = (request.Currency ?? "VND").Trim(),
            Proposal = request.Proposal?.Trim(),
            PrivacyMode = request.PrivacyMode,
            Status = RfpBidStatus.Submitted,
        };

        if (request.Items is { Count: > 0 })
        {
            foreach (var itemReq in request.Items)
            {
                var bidItem = new RfpBidItem
                {
                    RfpItemId = itemReq.RfpItemId,
                    CompanyId = request.CompanyId,
                    Brand = (itemReq.Brand ?? string.Empty).Trim(),
                    Quantity = itemReq.Quantity,
                    UnitPrice = itemReq.UnitPrice,
                    TotalPrice = itemReq.TotalPrice,
                    Currency = (request.Currency ?? "VND").Trim(),
                    Note = itemReq.Note?.Trim(),
                    Status = RfpBidItemStatus.Active,
                };

                if (itemReq.Specs is { Count: > 0 })
                {
                    foreach (var specReq in itemReq.Specs)
                    {
                        bidItem.Specs.Add(new RfpBidItemSpec
                        {
                            Key = specReq.Key.Trim(),
                            ValueText = specReq.ValueText?.Trim(),
                            ValueNumber = specReq.ValueNumber,
                            ValueBoolean = specReq.ValueBoolean,
                            Unit = specReq.Unit?.Trim(),
                            Status = RfpBidItemSpecStatus.Active,
                        });
                    }
                }

                bid.Items.Add(bidItem);
            }
        }

        // Update participation status to Accepted
        if (participation.Status == VendorParticipationStatus.Invited)
        {
            participation.Status = VendorParticipationStatus.Accepted;
            participation.ResponseAt = DateTime.UtcNow;
        }

        dbContext.RfpBids.Add(bid);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/vendor/bids/{bid.Id}", new { bid.Id });
    }

    // ── GET /api/vendor/bids ──────────────────────────────────────────────────────

    private static async Task<IResult> ListMyBidsAsync(
        [FromQuery] int? companyId,
        [FromQuery] int? rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.RfpBids.AsNoTracking().AsQueryable();

        if (companyId.HasValue)
            query = query.Where(b => b.CompanyId == companyId.Value);
        if (rfpId.HasValue)
            query = query.Where(b => b.RfpId == rfpId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BidListItem(
                b.Id, b.RfpId, b.CompanyId,
                b.SubTotal, b.GrandTotal, b.Currency,
                (int)b.Status, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(new BidListResponse(total, page, pageSize, items));
    }

    // ── GET /api/vendor/bids/{bidId} ──────────────────────────────────────────────

    private static async Task<IResult> GetBidDetailAsync(
        [FromRoute] int bidId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var bid = await dbContext.RfpBids.AsNoTracking()
            .Include(b => b.Items).ThenInclude(i => i.Specs)
            .FirstOrDefaultAsync(b => b.Id == bidId, cancellationToken);

        if (bid is null)
            return Results.NotFound();

        var detail = new BidDetailResponse(
            bid.Id, bid.RfpId, bid.CompanyId,
            bid.VatRate, bid.SubTotal, bid.GrandTotal, bid.Currency,
            bid.Proposal, (int)bid.PrivacyMode, (int)bid.Status,
            bid.CreatedAtUtc, bid.CreatedBy, bid.UpdatedAtUtc, bid.UpdatedBy,
            bid.Items.Select(i => new BidItemDto(
                i.Id, i.RfpItemId, i.CompanyId, i.Brand,
                i.Quantity, i.UnitPrice, i.TotalPrice, i.Currency,
                i.Note, (int)i.Status,
                i.Specs.Select(s => new BidItemSpecDto(
                    s.Id, s.Key, s.ValueText, s.ValueNumber, s.ValueBoolean, s.Unit, (int)s.Status
                )).ToList()
            )).ToList());

        return Results.Ok(detail);
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateBidRequest(
    int RfpId, int CompanyId,
    decimal VatRate, decimal SubTotal, decimal GrandTotal,
    string? Currency, string? Proposal,
    RfpBidPrivacyMode PrivacyMode,
    List<CreateBidItemRequest>? Items);

public sealed record CreateBidItemRequest(
    int RfpItemId, string? Brand,
    int Quantity, decimal UnitPrice, decimal TotalPrice,
    string? Note,
    List<CreateBidItemSpecRequest>? Specs);

public sealed record CreateBidItemSpecRequest(
    string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean, string? Unit);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record BidListItem(
    int Id, int RfpId, int CompanyId,
    decimal SubTotal, decimal GrandTotal, string Currency,
    int Status, DateTime CreatedAtUtc);

public sealed record BidListResponse(
    int Total, int Page, int PageSize, IReadOnlyCollection<BidListItem> Items);

public sealed record BidDetailResponse(
    int Id, int RfpId, int CompanyId,
    decimal VatRate, decimal SubTotal, decimal GrandTotal, string Currency,
    string? Proposal, int PrivacyMode, int Status,
    DateTime CreatedAtUtc, string CreatedBy, DateTime UpdatedAtUtc, string UpdatedBy,
    IReadOnlyCollection<BidItemDto> Items);

public sealed record BidItemDto(
    int Id, int RfpItemId, int CompanyId, string Brand,
    int Quantity, decimal UnitPrice, decimal TotalPrice, string Currency,
    string? Note, int Status,
    IReadOnlyCollection<BidItemSpecDto> Specs);

public sealed record BidItemSpecDto(
    int Id, string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean,
    string? Unit, int Status);
