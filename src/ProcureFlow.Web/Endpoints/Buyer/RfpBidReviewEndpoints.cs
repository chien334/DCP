using ProcureFlow.Infrastructure.Data;
using ProcureFlow.Web.Endpoints.Vendor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Buyer;

public static class RfpBidReviewEndpoints
{
    public static RouteGroupBuilder MapRfpBidReviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rfps/{rfpId:int}/bids", ListBidsByRfpAsync);
        group.MapGet("/rfps/{rfpId:int}/bids/{bidId:int}", GetBidByRfpAsync);
        group.MapGet("/rfps/{rfpId:int}/comparison", CompareBidsAsync);
        return group;
    }

    // ── GET /api/buyer/rfps/{rfpId}/bids ──────────────────────────────────────────

    private static async Task<IResult> ListBidsByRfpAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfpExists = await dbContext.Rfps.AnyAsync(r => r.Id == rfpId, cancellationToken);
        if (!rfpExists)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        var bids = await dbContext.RfpBids.AsNoTracking()
            .Where(b => b.RfpId == rfpId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BidListItem(
                b.Id, b.RfpId, b.CompanyId,
                b.SubTotal, b.GrandTotal, b.Currency,
                (int)b.Status, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(bids);
    }

    // ── GET /api/buyer/rfps/{rfpId}/bids/{bidId} ─────────────────────────────────

    private static async Task<IResult> GetBidByRfpAsync(
        [FromRoute] int rfpId,
        [FromRoute] int bidId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var bid = await dbContext.RfpBids.AsNoTracking()
            .Include(b => b.Items).ThenInclude(i => i.Specs)
            .FirstOrDefaultAsync(b => b.Id == bidId && b.RfpId == rfpId, cancellationToken);

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

    // ── GET /api/buyer/rfps/{rfpId}/comparison ────────────────────────────────────

    private static async Task<IResult> CompareBidsAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfp = await dbContext.Rfps.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Specs)
            .FirstOrDefaultAsync(r => r.Id == rfpId, cancellationToken);

        if (rfp is null)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        var bids = await dbContext.RfpBids.AsNoTracking()
            .Include(b => b.Items).ThenInclude(i => i.Specs)
            .Where(b => b.RfpId == rfpId)
            .OrderBy(b => b.CompanyId)
            .ToListAsync(cancellationToken);

        var vendors = bids.Select(b => new ComparisonVendorSummary(
            b.Id, b.CompanyId, b.SubTotal, b.GrandTotal, b.Currency,
            b.VatRate, (int)b.Status)).ToList();

        var items = rfp.Items.Select(rfpItem =>
        {
            var rfpSpecs = rfpItem.Specs.Select(s => new ComparisonSpecDto(
                s.Key, s.ValueText, s.ValueNumber, s.ValueBoolean, s.Unit)).ToList();

            var vendorBids = bids.Select(bid =>
            {
                var bidItem = bid.Items.FirstOrDefault(bi => bi.RfpItemId == rfpItem.Id);
                if (bidItem is null)
                    return new ComparisonBidItemDto(bid.Id, bid.CompanyId, null, null, null, null, null, null, new List<ComparisonSpecDto>());

                var bidSpecs = bidItem.Specs.Select(s => new ComparisonSpecDto(
                    s.Key, s.ValueText, s.ValueNumber, s.ValueBoolean, s.Unit)).ToList();

                return new ComparisonBidItemDto(
                    bid.Id, bid.CompanyId, bidItem.Brand,
                    bidItem.Quantity, bidItem.UnitPrice, bidItem.TotalPrice,
                    bidItem.Currency, bidItem.Note, bidSpecs);
            }).ToList();

            return new ComparisonItemRow(
                rfpItem.Id, rfpItem.Name, rfpItem.Quantity, rfpItem.Unit, rfpItem.Note,
                rfpSpecs, vendorBids);
        }).ToList();

        return Results.Ok(new BidComparisonResponse(rfpId, rfp.Title, vendors, items));
    }
}

// ── Comparison DTOs ───────────────────────────────────────────────────────────

public sealed record BidComparisonResponse(
    int RfpId, string RfpTitle,
    IReadOnlyCollection<ComparisonVendorSummary> Vendors,
    IReadOnlyCollection<ComparisonItemRow> Items);

public sealed record ComparisonVendorSummary(
    int BidId, int CompanyId,
    decimal SubTotal, decimal GrandTotal, string Currency,
    decimal VatRate, int Status);

public sealed record ComparisonItemRow(
    int RfpItemId, string Name, int Quantity, string Unit, string? Note,
    IReadOnlyCollection<ComparisonSpecDto> RequiredSpecs,
    IReadOnlyCollection<ComparisonBidItemDto> Bids);

public sealed record ComparisonBidItemDto(
    int BidId, int CompanyId,
    string? Brand, int? Quantity, decimal? UnitPrice, decimal? TotalPrice,
    string? Currency, string? Note,
    IReadOnlyCollection<ComparisonSpecDto> Specs);

public sealed record ComparisonSpecDto(
    string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean, string? Unit);
