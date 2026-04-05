using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Vendor;

public static class VendorContractEndpoints
{
    public static RouteGroupBuilder MapVendorContractEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/contracts/{contractId:int}", GetVendorContractAsync);
        group.MapPost("/contracts/{contractId:int}/sign", VendorSignContractAsync);
        group.MapPost("/contracts/{contractId:int}/decline", VendorDeclineContractAsync);
        return group;
    }

    private static async Task<IResult> GetVendorContractAsync(
        [FromRoute] int contractId,
        [FromQuery] int companyId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts.AsNoTracking()
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        if (contract.RfpFinalize.CompanyId != companyId)
            return Results.Forbid();

        return Results.Ok(new VendorContractDetailResponse(
            contract.Id,
            contract.RfpFinalizeId,
            contract.RfpFinalize.RfpId,
            contract.ContractNo,
            contract.Title,
            contract.FileUrl,
            contract.BuyerSign,
            contract.BuyerSignAt,
            contract.VendorSign,
            contract.VendorSignAt,
            contract.Note,
            (int)contract.Status,
            contract.CreatedAtUtc,
            contract.CreatedBy,
            contract.UpdatedAtUtc,
            contract.UpdatedBy));
    }

    private static async Task<IResult> VendorSignContractAsync(
        [FromRoute] int contractId,
        [FromBody] VendorActionRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        if (contract.RfpFinalize.CompanyId != request.CompanyId)
            return Results.Forbid();

        if (contract.Status == RfpContractStatus.Declined)
            return Results.Conflict(new { code = "CONTRACT_ALREADY_DECLINED" });

        if (contract.VendorSign)
            return Results.Conflict(new { code = "VENDOR_ALREADY_SIGNED" });

        contract.VendorSign = true;
        contract.VendorSignAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
            contract.Note = request.Note.Trim();

        contract.Status = contract.BuyerSign
            ? RfpContractStatus.Signed
            : RfpContractStatus.PartiallySigned;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { contract.Id, status = (int)contract.Status });
    }

    private static async Task<IResult> VendorDeclineContractAsync(
        [FromRoute] int contractId,
        [FromBody] VendorActionRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        if (contract.RfpFinalize.CompanyId != request.CompanyId)
            return Results.Forbid();

        if (contract.VendorSign)
            return Results.Conflict(new { code = "VENDOR_ALREADY_SIGNED" });

        if (contract.Status == RfpContractStatus.Signed)
            return Results.Conflict(new { code = "CONTRACT_ALREADY_SIGNED" });

        contract.Status = RfpContractStatus.Declined;
        contract.Note = request.Note?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { contract.Id, status = (int)contract.Status });
    }
}

public sealed record VendorActionRequest(int CompanyId, string? Note);

public sealed record VendorContractDetailResponse(
    int Id,
    int RfpFinalizeId,
    int RfpId,
    string ContractNo,
    string Title,
    string FileUrl,
    bool BuyerSign,
    DateTime? BuyerSignAt,
    bool VendorSign,
    DateTime? VendorSignAt,
    string? Note,
    int Status,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime UpdatedAtUtc,
    string UpdatedBy);
