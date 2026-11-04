namespace Tumiia.Domain.Audit;

/// <summary>One security event, such as a login or a password reset. Rows are only ever added, never changed.</summary>
public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The user the event is about, or null when it is not known (for example a login with an unknown email).</summary>
    public Guid? UserId { get; private set; }

    public AuditEventType EventType { get; private set; }

    /// <summary>The client's IP address, as worked out by the API.</summary>
    public string IpAddress { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Whether the action worked (for example false for a failed login).</summary>
    public bool Success { get; private set; }

    /// <summary>Records a new event.</summary>
    public static AuditEvent Record(AuditEventType eventType, Guid? userId, string ipAddress, bool success, DateTimeOffset now)
    {
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            IpAddress = ipAddress,
            OccurredAt = now,
            Success = success,
        };
    }
}
