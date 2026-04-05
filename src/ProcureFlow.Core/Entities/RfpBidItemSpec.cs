namespace ProcureFlow.Core.Entities;

public class RfpBidItemSpec : IAuditable
{
    public int Id { get; set; }
    public int RfpBidItemId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBoolean { get; set; }
    public string? Unit { get; set; }
    public RfpBidItemSpecStatus Status { get; set; } = RfpBidItemSpecStatus.Active;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation
    public RfpBidItem RfpBidItem { get; set; } = default!;
}

public enum RfpBidItemSpecStatus { Active = 1, Inactive = 2 }
