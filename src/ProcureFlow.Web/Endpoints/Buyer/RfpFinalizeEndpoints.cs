using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Buyer;

public static class RfpFinalizeEndpoints
{
    public static RouteGroupBuilder MapRfpFinalizeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rfps/{rfpId:int}/finalize", FinalizeBidAsync);
        group.MapGet("/rfps/{rfpId:int}/finalize", GetFinalizeByRfpAsync);
        return group;
    }

    private static async Task<IResult> FinalizeBidAsync(
        [FromRoute] int rfpId,
        [FromBody] FinalizeBidRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfp = await dbContext.Rfps
            .FirstOrDefaultAsync(r => r.Id == rfpId, cancellationToken);
        if (rfp is null)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        var existingFinalize = await dbContext.RfpFinalizes
            .AnyAsync(f => f.RfpId == rfpId, cancellationToken);
        if (existingFinalize)
            return Results.Conflict(new { code = "RFP_ALREADY_FINALIZED" });

        if (rfp.Status is RfpStatus.Canceled or RfpStatus.Closed)
            return Results.Conflict(new { code = "RFP_STATUS_NOT_ALLOWED" });

        var winningBid = await dbContext.RfpBids
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.RfpBidId && b.RfpId == rfpId, cancellationToken);

        if (winningBid is null)
            return Results.UnprocessableEntity(new { code = "INVALID_BID_FOR_RFP" });

        if (winningBid.Status != RfpBidStatus.Submitted)
            return Results.UnprocessableEntity(new { code = "BID_STATUS_NOT_ALLOWED" });

        if (winningBid.Items.Count == 0)
            return Results.UnprocessableEntity(new { code = "BID_ITEMS_REQUIRED" });

        var finalize = new RfpFinalize
        {
            RfpId = rfpId,
            RfpBidId = winningBid.Id,
            CompanyId = winningBid.CompanyId,
            VatRate = winningBid.VatRate,
            SubTotal = winningBid.SubTotal,
            GrandTotal = winningBid.GrandTotal,
            Currency = winningBid.Currency,
            Note = request.Note?.Trim(),
            Status = RfpFinalizeStatus.Finalized,
            Items = winningBid.Items.Select(i => new RfpFinalizeItem
            {
                RfpItemId = i.RfpItemId,
                RfpBidItemId = i.Id,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Currency = i.Currency,
                Status = RfpFinalizeItemStatus.Active,
            }).ToList()
        };

        rfp.Status = RfpStatus.Closed;

        dbContext.RfpFinalizes.Add(finalize);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/buyer/rfps/{rfpId}/finalize", new { finalize.Id });
    }

    private static async Task<IResult> GetFinalizeByRfpAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var finalize = await dbContext.RfpFinalizes.AsNoTracking()
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.RfpId == rfpId, cancellationToken);

        if (finalize is null)
            return Results.NotFound(new { code = "FINALIZE_NOT_FOUND" });

        var response = new FinalizeDetailResponse(
            finalize.Id,
            finalize.RfpId,
            finalize.RfpBidId,
            finalize.CompanyId,
            finalize.VatRate,
            finalize.SubTotal,
            finalize.GrandTotal,
            finalize.Currency,
            finalize.Note,
            (int)finalize.Status,
            finalize.CreatedAtUtc,
            finalize.CreatedBy,
            finalize.Items.Select(i => new FinalizeItemDto(
                i.Id,
                i.RfpItemId,
                i.RfpBidItemId,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice,
                i.Currency,
                (int)i.Status)).ToList());

        return Results.Ok(response);
    }
}

public sealed record FinalizeBidRequest(int RfpBidId, string? Note);

public sealed record FinalizeDetailResponse(
    int Id,
    int RfpId,
    int RfpBidId,
    int CompanyId,
    decimal VatRate,
    decimal SubTotal,
    decimal GrandTotal,
    string Currency,
    string? Note,
    int Status,
    DateTime CreatedAtUtc,
    string CreatedBy,
    IReadOnlyCollection<FinalizeItemDto> Items);

public sealed record FinalizeItemDto(
    int Id,
    int RfpItemId,
    int RfpBidItemId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string Currency,
    int Status);
