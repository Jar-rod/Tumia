using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>
/// The <c>document_analyses</c> table. Signals, fields, warnings and the analyser are <c>jsonb</c>. A document has at
/// most one analysis per pipeline version, which is what makes a repeated queue message harmless. Rows are never
/// updated, so the JSON values are compared by reference only.
/// </summary>
public sealed class DocumentAnalysisConfiguration : IEntityTypeConfiguration<DocumentAnalysis>
{
    /// <summary>Maps the analysis to its table.</summary>
    public void Configure(EntityTypeBuilder<DocumentAnalysis> builder)
    {
        builder.ToTable("document_analyses");
        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.PipelineVersion).HasMaxLength(20).IsRequired();
        builder.Property(analysis => analysis.DetectedLanguage).HasMaxLength(10).IsRequired();
        builder.Property(analysis => analysis.ContentTypeDetected).HasMaxLength(255).IsRequired();
        builder.Property(analysis => analysis.ProposedCategory).HasMaxLength(50).IsRequired();
        builder.Property(analysis => analysis.ProposedCategoryName).HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.RunnerUpCategory).HasMaxLength(50);
        builder.Property(analysis => analysis.Band).HasConversion<string>().HasMaxLength(10);
        builder.Property(analysis => analysis.Outcome).HasConversion<string>().HasMaxLength(30);
        builder.Property(analysis => analysis.Summary).HasMaxLength(500).IsRequired();
        Json(builder.Property(analysis => analysis.Signals));
        Json(builder.Property(analysis => analysis.Fields));
        Json(builder.Property(analysis => analysis.Warnings));
        Json(builder.Property(analysis => analysis.Analyser));

        builder.HasIndex(analysis => new { analysis.DocumentId, analysis.PipelineVersion }).IsUnique();
        builder.HasOne<Document>().WithMany().HasForeignKey(analysis => analysis.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void Json<T>(PropertyBuilder<T> property)
    {
        property
            .HasColumnType("jsonb")
            .HasConversion(value => AnalysisJson.Write(value), json => AnalysisJson.Read<T>(json))
            .Metadata.SetValueComparer(new ValueComparer<T>((first, second) => ReferenceEquals(first, second), value => value == null ? 0 : value.GetHashCode(), value => value));
    }
}
