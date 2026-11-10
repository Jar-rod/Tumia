namespace Tumiia.Domain.Analysis;

/// <summary>The category the keyword rules point to, and the runner-up.</summary>
/// <param name="WinnerKey">The rules' category, or Needs Review when no keyword matched.</param>
/// <param name="WinnerProbability">The winner's probability (0 to 1).</param>
/// <param name="RunnerUpKey">The second most likely category, if any.</param>
/// <param name="RunnerUpProbability">The runner-up's probability.</param>
public sealed record CategoryChoice(string WinnerKey, double WinnerProbability, string? RunnerUpKey, double RunnerUpProbability)
{
    /// <summary>True when at least one keyword, alias or pattern matched.</summary>
    public bool HasOpinion => WinnerKey != TaxonomyCategory.NeedsReviewKey;
}
