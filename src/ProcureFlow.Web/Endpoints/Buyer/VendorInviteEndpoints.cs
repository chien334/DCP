using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Buyer;

public static class VendorInviteEndpoints
{
    public static RouteGroupBuilder MapVendorInviteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rfps/{rfpId:int}/invites", InviteVendorAsync);
        group.MapGet("/rfps/{rfpId:int}/invites", ListInvitesAsync);
        return group;
    }

    // ── POST /api/buyer/rfps/{rfpId}/invites ──────────────────────────────────────

    private static async Task<IResult> InviteVendorAsync(
        [FromRoute] int rfpId,
        [FromBody] InviteVendorRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfpExists = await dbContext.Rfps.AnyAsync(r => r.Id == rfpId, cancellationToken);
        if (!rfpExists)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists)
            return Results.NotFound(new { code = "COMPANY_NOT_FOUND" });

        var alreadyInvited = await dbContext.RfpVendorParticipations
            .AnyAsync(v => v.RfpId == rfpId && v.CompanyId == request.CompanyId, cancellationToken);
        if (alreadyInvited)
            return Results.Conflict(new { code = "ALREADY_INVITED" });

        var participation = new RfpVendorParticipation
        {
            RfpId = rfpId,
            CompanyId = request.CompanyId,
            Status = VendorParticipationStatus.Invited,
            InviteAt = DateTime.UtcNow,
        };

        dbContext.RfpVendorParticipations.Add(participation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/buyer/rfps/{rfpId}/invites", new { participation.Id });
    }

    // ── GET /api/buyer/rfps/{rfpId}/invites ───────────────────────────────────────

    private static async Task<IResult> ListInvitesAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rfpExists = await dbContext.Rfps.AnyAsync(r => r.Id == rfpId, cancellationToken);
        if (!rfpExists)
            return Results.NotFound(new { code = "RFP_NOT_FOUND" });

        var invites = await dbContext.RfpVendorParticipations.AsNoTracking()
            .Where(v => v.RfpId == rfpId)
            .OrderByDescending(v => v.InviteAt)
            .Select(v => new VendorInviteDto(
                v.Id, v.RfpId, v.CompanyId,
                (int)v.Status, v.InviteAt, v.ResponseAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(invites);
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record InviteVendorRequest(int CompanyId);

public sealed record VendorInviteDto(
    int Id, int RfpId, int CompanyId,
    int Status, DateTime InviteAt, DateTime? ResponseAt);
