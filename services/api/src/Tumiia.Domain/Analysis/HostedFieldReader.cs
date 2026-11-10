using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Checks the facts the hosted AI reported before TUMIIA keeps them. Amounts, dates and currencies must read
/// properly (they are rewritten in TUMIIA's formats) or the fact is dropped. When the document has typed text, the
/// quoted snippet must really be in it; if not, the fact is kept with half its confidence and a note, because the
/// AI may have misread or invented it.
/// </summary>
public static partial class HostedFieldReader
{
    private const double UnverifiedPenalty = 0.5;
    private const int MaxValueLength = 200;
    private const int MaxSnippetLength = 120;

    /// <summary>The checked fields, and a warning for each fact that was dropped.</summary>
    public static (IReadOnlyList<ExtractedField> Fields, IReadOnlyList<string> Warnings) Read(IReadOnlyList<HostedField> reported, DocumentText text)
    {
        var fields = new List<ExtractedField>();
        var warnings = new List<string>();
        var searchableText = Squash(text.AllText);

        foreach (var field in reported.Where(field => !string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.Value)))
        {
            var name = field.Name.Trim().ToLowerInvariant();
            if (name == FieldNames.DocumentType || fields.Any(existing => existing.Name == name))
            {
                continue;
            }

            var value = Normalise(name, field.Value.Trim());
            if (value is null)
            {
                warnings.Add($"The AI's {name.Replace('_', ' ')} (\"{Truncate(field.Value, 40)}\") could not be read, so it was left out.");
                continue;
            }

            fields.Add(ToExtracted(name, value, field, searchableText));
        }

        return (fields, warnings);
    }

    /// <summary>The value in TUMIIA's format, or null when it is not a valid value of its kind.</summary>
    public static string? Normalise(string name, string value)
    {
        switch (name)
        {
            case FieldNames.Amount:
                var money = MoneyFinder.FindFirst(value);
                return money is null ? null : MoneyFinder.Format(money.Amount);
            case FieldNames.Currency:
                var code = value.ToUpperInvariant();
                return CurrencyCode().IsMatch(code) ? code : null;
            case FieldNames.DocumentDate:
                var dates = DateFinder.FindAll(value);
                return dates.Count == 0 ? null : DateFinder.Format(dates[0].Date);
            default:
                return Truncate(value, MaxValueLength);
        }
    }

    private static ExtractedField ToExtracted(string name, string value, HostedField field, string searchableText)
    {
        var confidence = Math.Clamp(field.Confidence, 0, 1);
        var snippet = Truncate(field.Snippet.Trim(), MaxSnippetLength);
        var unverified = searchableText.Length > 0 && snippet.Length > 0 && !searchableText.Contains(Squash(snippet), StringComparison.Ordinal);
        if (unverified)
        {
            confidence *= UnverifiedPenalty;
            snippet += " (not found in the document's text)";
        }

        var evidence = new FieldEvidence("bedrock", field.Page > 0 ? field.Page : null, null, snippet.Length > 0 ? snippet : null);
        return new ExtractedField(name, value, confidence, evidence, FieldMethods.Hosted);
    }

    // Lower case with all spacing removed, so "Total:  R 18 450" matches "total: R18 450" across line breaks.
    private static string Squash(string text) => new(text.ToLowerInvariant().Where(letter => !char.IsWhiteSpace(letter)).ToArray());

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex CurrencyCode();
}
