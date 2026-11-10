using System.Text.Json;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>The starting taxonomy, from <c>Seed/taxonomy.json</c> built into this assembly (the v1.1 categories).</summary>
public static class TaxonomySeed
{
    private const string ResourceName = "Tumiia.Infrastructure.Seed.taxonomy.json";

    /// <summary>The seed categories, in order.</summary>
    public static IReadOnlyList<TaxonomyCategory> Load()
    {
        using var stream = typeof(TaxonomySeed).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The {ResourceName} resource is missing from the build.");
        return Parse(stream);
    }

    /// <summary>Reads a taxonomy file: <c>{ "categories": [ ... ] }</c>.</summary>
    public static IReadOnlyList<TaxonomyCategory> Parse(Stream json)
    {
        var file = JsonSerializer.Deserialize<TaxonomyFile>(json, AnalysisJson.Options);
        return file?.Categories ?? [];
    }

    private sealed record TaxonomyFile(IReadOnlyList<TaxonomyCategory> Categories);
}
