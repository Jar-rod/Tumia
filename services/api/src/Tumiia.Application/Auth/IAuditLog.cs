using Tumiia.Domain.Audit;

namespace Tumiia.Application.Auth;

/// <summary>Writes security events to the <c>audit_events</c> table.</summary>
public interface IAuditLog
{
    /// <summary>Records one event. Pass <paramref name="documentId"/> for document events.</summary>
    Task RecordAsync(AuditEventType eventType, Guid? userId, string ipAddress, bool success, CancellationToken cancellationToken, Guid? documentId = null);
}
