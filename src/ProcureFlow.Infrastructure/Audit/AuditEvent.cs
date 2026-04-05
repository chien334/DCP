namespace ProcureFlow.Infrastructure.Audit;

/// <summary>
/// Immutable record of a write operation for audit trail purposes.
/// Contains only identifiers and metadata — no sensitive payload bodies.
/// </summary>
public sealed record AuditEvent(
    string EntityType,
    string EntityId,
    string Operation,
    string Actor,
    DateTime OccurredAtUtc);
