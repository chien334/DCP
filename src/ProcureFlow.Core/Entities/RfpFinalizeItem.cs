namespace ProcureFlow.Core.Entities;

public enum RfpFinalizeItemStatus
{
    Active = 1,
    Inactive = 2,
}

public class RfpFinalizeItem : IAuditable
{
    public int Id { get; set; }
    public int RfpFinalizeId { get; set; }
    public int RfpItemId { get; set; }
    public int RfpBidItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public RfpFinalizeItemStatus Status { get; set; } = RfpFinalizeItemStatus.Active;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public RfpFinalize RfpFinalize { get; set; } = default!;
    public RfpItem RfpItem { get; set; } = default!;
    public RfpBidItem RfpBidItem { get; set; } = default!;
}
