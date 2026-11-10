namespace Tumiia.Domain.Analysis;

/// <summary>Picks the winner and the runner-up from the keyword rules' probabilities. With no match, Needs Review.</summary>
public static class CategoryChooser
{
    /// <summary>The rules' choice.</summary>
    public static CategoryChoice Choose(IReadOnlyDictionary<string, double> rules)
    {
        var ranked = rules
            .OrderByDescending(category => category.Value)
            .ThenBy(category => category.Key, StringComparer.Ordinal)
            .ToList();

        if (ranked.Count == 0)
        {
            return new CategoryChoice(TaxonomyCategory.NeedsReviewKey, 0, null, 0);
        }

        var runnerUp = ranked.Count > 1 ? ranked[1] : default;
        return new CategoryChoice(ranked[0].Key, ranked[0].Value, runnerUp.Key, runnerUp.Value);
    }
}
