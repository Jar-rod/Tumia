using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.Infrastructure.Audit;

/// <summary>Writes security events to the <c>audit_events</c> table.</summary>
public sealed class EfAuditLog(TumiiaDbContext database, TimeProvider clock) : IAuditLog
{
    /// <inheritdoc />
    public async Task RecordAsync(AuditEventType eventType, Guid? userId, string ipAddress, bool success, CancellationToken cancellationToken)
    {
        database.AuditEvents.Add(AuditEvent.Record(eventType, userId, ipAddress, success, clock.GetUtcNow()));
        await database.SaveChangesAsync(cancellationToken);
    }
}
