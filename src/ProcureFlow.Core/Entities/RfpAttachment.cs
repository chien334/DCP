namespace ProcureFlow.Core.Entities;

public enum RfpAttachmentStatus
{
    Active = 1,
    Inactive = 2,
}

public class RfpAttachment : IAuditable
{
    public int Id { get; set; }
    public int RfpId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public RfpAttachmentStatus Status { get; set; } = RfpAttachmentStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public Rfp Rfp { get; set; } = default!;
}
