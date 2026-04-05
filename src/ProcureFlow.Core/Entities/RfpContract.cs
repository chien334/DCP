namespace ProcureFlow.Core.Entities;

public enum RfpContractStatus
{
    Created = 1,
    PartiallySigned = 2,
    Signed = 3,
    Declined = 4,
}

public class RfpContract : IAuditable
{
    public int Id { get; set; }
    public int RfpFinalizeId { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public bool BuyerSign { get; set; }
    public DateTime? BuyerSignAt { get; set; }
    public bool VendorSign { get; set; }
    public DateTime? VendorSignAt { get; set; }
    public string? Note { get; set; }
    public RfpContractStatus Status { get; set; } = RfpContractStatus.Created;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public RfpFinalize RfpFinalize { get; set; } = default!;
}
