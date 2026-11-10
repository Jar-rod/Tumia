using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>The <c>taxonomy_categories</c> table, keyed by category key, with the definition as <c>jsonb</c>.</summary>
public sealed class TaxonomyCategoryConfiguration : IEntityTypeConfiguration<TaxonomyCategoryRecord>
{
    /// <summary>Maps the category record to its table.</summary>
    public void Configure(EntityTypeBuilder<TaxonomyCategoryRecord> builder)
    {
        builder.ToTable("taxonomy_categories");
        builder.HasKey(record => record.Key);
        builder.Property(record => record.Key).HasMaxLength(50);
        builder.Property(record => record.Definition).HasColumnType("jsonb").IsRequired();
    }
}
