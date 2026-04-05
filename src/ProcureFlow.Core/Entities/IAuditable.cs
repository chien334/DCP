namespace ProcureFlow.Core.Entities;

/// <summary>
/// Marks an entity that the AuditStampInterceptor should automatically stamp.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string CreatedBy { get; set; }
    DateTime UpdatedAtUtc { get; set; }
    string UpdatedBy { get; set; }
}
