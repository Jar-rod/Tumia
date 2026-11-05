using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Projects;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>
/// The <c>documents</c> table. Statuses and levels are stored as text so the table reads well on its own;
/// <c>confirmed_fields</c> is <c>jsonb</c>. The same file (SHA-256) can only be in a project once. Deleting a
/// project or user is refused while documents point at it, so no document row is ever lost by accident.
/// </summary>
public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    /// <summary>Maps the document to its table.</summary>
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(document => document.OriginalFilename).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentTypeClaimed).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentTypeDetected).HasMaxLength(255);
        builder.Property(document => document.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(document => document.ConsequenceLevel).HasConversion<string>().HasMaxLength(10);
        builder.Property(document => document.CaptureSource).HasConversion(source => ToText(source), text => FromText(text)).HasMaxLength(20);
        builder.Property(document => document.DocumentTypeConfirmed).HasMaxLength(100);
        builder.Property(document => document.SubjectConfirmed).HasMaxLength(200);
        builder.Property(document => document.ConfirmedFields).HasColumnType("jsonb");

        builder.HasIndex(document => document.StorageKey).IsUnique();
        builder.HasIndex(document => new { document.ProjectId, document.Sha256 }).IsUnique();
        builder.HasIndex(document => new { document.ProjectId, document.Status });
        builder.HasOne<Project>().WithMany().HasForeignKey(document => document.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(document => document.UploadedBy).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>The capture source as the v1.1 record writes it: <c>drag_drop</c>, <c>file_picker</c>, <c>mobile_upload</c>.</summary>
    public static string ToText(CaptureSource source) => source switch
    {
        CaptureSource.DragDrop => "drag_drop",
        CaptureSource.FilePicker => "file_picker",
        CaptureSource.MobileUpload => "mobile_upload",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    /// <summary>Reads a stored capture source back.</summary>
    public static CaptureSource FromText(string text) => text switch
    {
        "drag_drop" => CaptureSource.DragDrop,
        "file_picker" => CaptureSource.FilePicker,
        "mobile_upload" => CaptureSource.MobileUpload,
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, null),
    };
}
