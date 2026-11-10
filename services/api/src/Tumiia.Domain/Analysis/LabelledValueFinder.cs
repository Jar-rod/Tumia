using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Finds the value that follows a label on the same line, as in "Supplier: ABC Supplies" or "Gross pay R 25 000".
/// Labels are matched as whole words, ignoring case.
/// </summary>
public static class LabelledValueFinder
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// The text after the first of <paramref name="labels"/> found in <paramref name="line"/>, or null. With
    /// <paramref name="separatorRequired"/>, the label must be followed by ":", "-", "#" or "=", so ordinary sentences
    /// ("a quote from ABC for bricks") are not read as labelled values.
    /// </summary>
    public static string? ValueAfterLabel(string line, IEnumerable<string> labels, bool separatorRequired = false)
    {
        var separator = separatorRequired ? @"\s*[:#\-–=]\s*" : @"\s*(?:[:#\-–=]\s*|\s+)";
        foreach (var label in labels)
        {
            var match = Regex.Match(line, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(label)}(?![\p{{L}}\p{{N}}]){separator}(?<value>\S.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);
            if (match.Success)
            {
                return match.Groups["value"].Value.Trim().TrimEnd('.', ',', ';');
            }
        }

        return null;
    }

    /// <summary>True when <paramref name="line"/> contains any of <paramref name="words"/> as a whole word.</summary>
    public static bool ContainsAny(string line, IEnumerable<string> words)
    {
        var lower = line.ToLowerInvariant();
        return words.Any(word => RulesClassifier.CountWord(lower, word.ToLowerInvariant()) > 0);
    }
}
