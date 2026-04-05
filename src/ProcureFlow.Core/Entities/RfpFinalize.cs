namespace ProcureFlow.Core.Entities;

public enum RfpFinalizeStatus
{
    Draft = 0,
    Finalized = 1,
    Canceled = 2,
}

public class RfpFinalize : IAuditable
{
    public int Id { get; set; }
    public int RfpId { get; set; }
    public int RfpBidId { get; set; }
    public int CompanyId { get; set; }
    public decimal VatRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Note { get; set; }
    public RfpFinalizeStatus Status { get; set; } = RfpFinalizeStatus.Finalized;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public Rfp Rfp { get; set; } = default!;
    public RfpBid RfpBid { get; set; } = default!;
    public Company Company { get; set; } = default!;
    public ICollection<RfpFinalizeItem> Items { get; set; } = new List<RfpFinalizeItem>();
    public RfpContract? Contract { get; set; }
}
