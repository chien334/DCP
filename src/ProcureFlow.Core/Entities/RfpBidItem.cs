namespace ProcureFlow.Core.Entities;

public class RfpBidItem : IAuditable
{
    public int Id { get; set; }
    public int RfpBidId { get; set; }
    public int RfpItemId { get; set; }
    public int CompanyId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Note { get; set; }
    public RfpBidItemStatus Status { get; set; } = RfpBidItemStatus.Active;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation
    public RfpBid RfpBid { get; set; } = default!;
    public RfpItem RfpItem { get; set; } = default!;
    public ICollection<RfpBidItemSpec> Specs { get; set; } = new List<RfpBidItemSpec>();
}

public enum RfpBidItemStatus { Active = 1, Inactive = 2 }
