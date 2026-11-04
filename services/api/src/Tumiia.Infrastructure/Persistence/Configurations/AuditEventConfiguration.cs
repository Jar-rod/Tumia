using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tumiia.Domain.Audit;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>
/// The <c>audit_events</c> table. The event type is stored as text so the table reads well on its own. There is
/// no foreign key to users, so the audit trail survives if an account is deleted.
/// </summary>
public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <summary>Maps the audit event to its table.</summary>
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(auditEvent => auditEvent.IpAddress).HasMaxLength(45);
        builder.HasIndex(auditEvent => auditEvent.UserId);
        builder.HasIndex(auditEvent => auditEvent.OccurredAt);
    }
}
