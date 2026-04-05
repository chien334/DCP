using Microsoft.Extensions.Logging;

namespace ProcureFlow.Infrastructure.Audit;

/// <summary>
/// Writes structured audit events to the application logger.
/// In a production system this would also persist to an audit table or external sink,
/// but structured log entries satisfy AUD-01 traceability requirements.
/// </summary>
public interface IAuditEventWriter
{
    void Write(AuditEvent auditEvent);
}

public sealed class AuditEventWriter : IAuditEventWriter
{
    private readonly ILogger<AuditEventWriter> _logger;

    public AuditEventWriter(ILogger<AuditEventWriter> logger)
    {
        _logger = logger;
    }

    public void Write(AuditEvent auditEvent)
    {
        _logger.LogInformation(
            "AUDIT entity={EntityType} id={EntityId} op={Operation} actor={Actor} at={OccurredAtUtc:O}",
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.Operation,
            auditEvent.Actor,
            auditEvent.OccurredAtUtc);
    }
}
