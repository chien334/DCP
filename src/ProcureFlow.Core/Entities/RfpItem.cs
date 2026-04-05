namespace ProcureFlow.Core.Entities;

public enum RfpItemStatus
{
    Active = 1,
    Inactive = 2,
}

public class RfpItem : IAuditable
{
    public int Id { get; set; }
    public int RfpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Note { get; set; }
    public RfpItemStatus Status { get; set; } = RfpItemStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public Rfp Rfp { get; set; } = default!;
    public ICollection<RfpItemSpec> Specs { get; set; } = new List<RfpItemSpec>();
}
