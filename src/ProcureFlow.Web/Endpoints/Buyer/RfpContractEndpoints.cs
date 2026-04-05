using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.Buyer;

public static class RfpContractEndpoints
{
    public static RouteGroupBuilder MapRfpContractEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rfps/{rfpId:int}/contract", CreateContractAsync);
        group.MapGet("/rfps/{rfpId:int}/contract", GetContractByRfpAsync);
        group.MapGet("/contracts/{contractId:int}", GetContractByIdAsync);
        group.MapPost("/contracts/{contractId:int}/sign", BuyerSignContractAsync);
        return group;
    }

    private static async Task<IResult> CreateContractAsync(
        [FromRoute] int rfpId,
        [FromBody] CreateContractRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var finalize = await dbContext.RfpFinalizes
            .FirstOrDefaultAsync(f => f.RfpId == rfpId, cancellationToken);
        if (finalize is null)
            return Results.NotFound(new { code = "FINALIZE_NOT_FOUND" });

        if (finalize.Status != RfpFinalizeStatus.Finalized)
            return Results.UnprocessableEntity(new { code = "FINALIZE_STATUS_NOT_ALLOWED" });

        var existingContract = await dbContext.RfpContracts
            .AnyAsync(c => c.RfpFinalizeId == finalize.Id, cancellationToken);
        if (existingContract)
            return Results.Conflict(new { code = "CONTRACT_ALREADY_EXISTS" });

        var contractNo = string.IsNullOrWhiteSpace(request.ContractNo)
            ? $"CT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20]
            : request.ContractNo.Trim();

        var contract = new RfpContract
        {
            RfpFinalizeId = finalize.Id,
            ContractNo = contractNo,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? $"Contract for RFP {rfpId}"
                : request.Title.Trim(),
            FileUrl = string.IsNullOrWhiteSpace(request.FileUrl)
                ? $"/contracts/{contractNo}.pdf"
                : request.FileUrl.Trim(),
            Note = request.Note?.Trim(),
            Status = RfpContractStatus.Created,
        };

        dbContext.RfpContracts.Add(contract);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/buyer/rfps/{rfpId}/contract", new { contract.Id });
    }

    private static async Task<IResult> GetContractByRfpAsync(
        [FromRoute] int rfpId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts.AsNoTracking()
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.RfpFinalize.RfpId == rfpId, cancellationToken);

        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        return Results.Ok(ToResponse(contract));
    }

    private static async Task<IResult> BuyerSignContractAsync(
        [FromRoute] int contractId,
        [FromBody] BuyerSignContractRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        if (contract.Status == RfpContractStatus.Declined)
            return Results.Conflict(new { code = "CONTRACT_ALREADY_DECLINED" });

        if (contract.BuyerSign)
            return Results.Conflict(new { code = "BUYER_ALREADY_SIGNED" });

        contract.BuyerSign = true;
        contract.BuyerSignAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
            contract.Note = request.Note.Trim();

        contract.Status = contract.VendorSign
            ? RfpContractStatus.Signed
            : RfpContractStatus.PartiallySigned;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(contract));
    }

    private static async Task<IResult> GetContractByIdAsync(
        [FromRoute] int contractId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.RfpContracts.AsNoTracking()
            .Include(c => c.RfpFinalize)
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return Results.NotFound(new { code = "CONTRACT_NOT_FOUND" });

        return Results.Ok(ToResponse(contract));
    }

    private static ContractDetailResponse ToResponse(RfpContract contract)
        => new(
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
            contract.UpdatedBy);
}

public sealed record CreateContractRequest(string? ContractNo, string? Title, string? FileUrl, string? Note);

public sealed record BuyerSignContractRequest(string? Note);

public sealed record ContractDetailResponse(
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
