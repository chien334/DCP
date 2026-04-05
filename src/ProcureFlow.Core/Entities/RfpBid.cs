namespace ProcureFlow.Core.Entities;

public class RfpBid : IAuditable
{
    public int Id { get; set; }
    public int RfpId { get; set; }
    public int CompanyId { get; set; }
    public decimal VatRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Proposal { get; set; }
    public RfpBidPrivacyMode PrivacyMode { get; set; } = RfpBidPrivacyMode.Standard;
    public RfpBidStatus Status { get; set; } = RfpBidStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation
    public Rfp Rfp { get; set; } = default!;
    public Company Company { get; set; } = default!;
    public ICollection<RfpBidItem> Items { get; set; } = new List<RfpBidItem>();
}

public enum RfpBidPrivacyMode { Standard = 1, Sealed = 2 }
public enum RfpBidStatus { Draft = 0, Submitted = 1, Withdrawn = 2 }
