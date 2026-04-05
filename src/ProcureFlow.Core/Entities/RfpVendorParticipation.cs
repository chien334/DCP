namespace ProcureFlow.Core.Entities;

public class RfpVendorParticipation : IAuditable
{
    public int Id { get; set; }
    public int RfpId { get; set; }
    public int CompanyId { get; set; }
    public VendorParticipationStatus Status { get; set; } = VendorParticipationStatus.Invited;
    public DateTime InviteAt { get; set; }
    public DateTime? ResponseAt { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation
    public Rfp Rfp { get; set; } = default!;
    public Company Company { get; set; } = default!;
}

public enum VendorParticipationStatus
{
    Invited = 1,
    Accepted = 2,
    Declined = 3
}
