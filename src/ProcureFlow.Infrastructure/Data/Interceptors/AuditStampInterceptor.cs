using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Audit;

namespace ProcureFlow.Infrastructure.Data.Interceptors;

/// <summary>Provides the current actor identity for audit stamping.</summary>
public interface IActorContextAccessor
{
    string Actor { get; }
}

public sealed class AuditStampInterceptor : SaveChangesInterceptor
{
    private readonly IActorContextAccessor _actorContext;
    private readonly IAuditEventWriter _auditEventWriter;

    public AuditStampInterceptor(IActorContextAccessor actorContext, IAuditEventWriter auditEventWriter)
    {
        _actorContext = actorContext;
        _auditEventWriter = auditEventWriter;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAndEmit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAndEmit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAndEmit(DbContext? context)
    {
        if (context is null) return;

        var actor = _actorContext.Actor;

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            var operation = entry.State switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                _ => null
            };

            if (operation is null) continue;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = actor;
            }

            entry.Entity.UpdatedAtUtc = now;
            entry.Entity.UpdatedBy = actor;

            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "?";

            _auditEventWriter.Write(new AuditEvent(
                EntityType: entry.Entity.GetType().Name,
                EntityId: entityId,
                Operation: operation,
                Actor: actor,
                OccurredAtUtc: now));
        }
    }
}
