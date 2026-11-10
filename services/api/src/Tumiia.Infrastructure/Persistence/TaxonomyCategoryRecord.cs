namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// One row of the <c>taxonomy_categories</c> table: a category's key, its place in the list, and its whole
/// definition (keywords, chips, fields, thresholds) as JSON, so a category can be changed without a migration.
/// </summary>
public sealed class TaxonomyCategoryRecord
{
    public string Key { get; set; } = string.Empty;

    /// <summary>The category's position in lists, starting at 0.</summary>
    public int SortOrder { get; set; }

    /// <summary>The <see cref="Domain.Analysis.TaxonomyCategory"/> as JSON.</summary>
    public string Definition { get; set; } = "{}";

    public DateTimeOffset UpdatedAt { get; set; }
}
