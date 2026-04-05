namespace ProcureFlow.Core.Entities;

public enum RfpType
{
    Standard = 1,
    Emergency = 2,
}

public enum RfpPrivacyMode
{
    Open = 1,
    Closed = 2,
}

public enum RfpStatus
{
    Draft = 0,
    Published = 1,
    InProgress = 2,
    Closed = 3,
    Canceled = 4,
}

public class Rfp : IAuditable
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public int CategoryId { get; set; }
    public string? Note { get; set; }
    public RfpType Type { get; set; } = RfpType.Standard;
    public RfpPrivacyMode PrivacyMode { get; set; } = RfpPrivacyMode.Open;
    public RfpStatus Status { get; set; } = RfpStatus.Draft;
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public Company Company { get; set; } = default!;
    public Category Category { get; set; } = default!;
    public ICollection<RfpItem> Items { get; set; } = new List<RfpItem>();
    public ICollection<RfpAttachment> Attachments { get; set; } = new List<RfpAttachment>();
}
