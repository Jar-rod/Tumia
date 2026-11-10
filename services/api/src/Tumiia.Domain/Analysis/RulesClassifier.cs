using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// The rules signal: scores each category by the keywords, aliases and patterns from its configuration that appear
/// in the document's text and file name. Scores are turned into probabilities that add up to 1. When nothing
/// matches at all, the result is empty, which means "the rules have no opinion".
/// </summary>
public static class RulesClassifier
{
    private const double KeywordWeight = 1.0;
    private const double PatternWeight = 2.0;
    private const double FileNameWeight = 1.5;
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(200);

    /// <summary>A probability per category key, for the categories with at least one match.</summary>
    public static IReadOnlyDictionary<string, double> Score(string text, string fileName, IReadOnlyList<TaxonomyCategory> categories)
    {
        var lowerText = text.ToLowerInvariant();
        var lowerName = NameAsWords(fileName);
        var scores = categories
            .Where(category => !category.IsNeedsReview)
            .Select(category => (category.Key, Score: ScoreCategory(category, text, lowerText, lowerName)))
            .Where(category => category.Score > 0)
            .ToList();

        var total = scores.Sum(category => category.Score);
        return scores.ToDictionary(category => category.Key, category => category.Score / total);
    }

    private static double ScoreCategory(TaxonomyCategory category, string text, string lowerText, string lowerName)
    {
        var score = 0.0;
        foreach (var word in category.Keywords.Concat(category.Aliases))
        {
            var lowerWord = word.ToLowerInvariant();
            score += Math.Min(CountWord(lowerText, lowerWord), 3) * KeywordWeight;
            if (CountWord(lowerName, lowerWord) > 0)
            {
                score += FileNameWeight;
            }
        }

        foreach (var pattern in category.Patterns)
        {
            score += Math.Min(CountPattern(text, pattern), 3) * PatternWeight;
        }

        return score;
    }

    /// <summary>How often <paramref name="word"/> appears as a whole word (or phrase) in <paramref name="text"/>.</summary>
    public static int CountWord(string text, string word)
    {
        if (word.Length == 0)
        {
            return 0;
        }

        var count = 0;
        var start = 0;
        while ((start = text.IndexOf(word, start, StringComparison.Ordinal)) >= 0)
        {
            var end = start + word.Length;
            var startsWord = start == 0 || !char.IsLetterOrDigit(text[start - 1]);
            var endsWord = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (startsWord && endsWord)
            {
                count++;
            }

            start = end;
        }

        return count;
    }

    // A bad pattern in configuration must never stop an analysis, so it simply counts as no match.
    private static int CountPattern(string text, string pattern)
    {
        try
        {
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, PatternTimeout).Count;
        }
        catch (Exception exception) when (exception is ArgumentException or RegexMatchTimeoutException)
        {
            return 0;
        }
    }

    // "Contractor_Quote-v2.pdf" becomes "contractor quote v2", so file names match keywords too.
    private static string NameAsWords(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        return new string(name.Select(letter => char.IsLetterOrDigit(letter) ? letter : ' ').ToArray());
    }
}
