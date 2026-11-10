using System.Globalization;
using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Finds the standard fields (party, date, amount and currency, subject, work package, site reference, location,
/// language) and any extra fields the category's configuration defines. Every field gets its own confidence: how
/// reliable the way it was found is, times how well that line was read. A value that is not there is left out;
/// nothing is ever guessed.
/// </summary>
public static partial class FieldExtractor
{
    private const int MaxValueLength = 200;
    private const int MaxSnippetLength = 120;

    private static readonly string[] TotalWords = ["total", "amount due", "amount paid", "grand total", "balance due", "total due", "amount", "to pay", "paid", "sum of"];
    private static readonly string[] DateLabels = ["date", "dated", "invoice date", "quote date", "issue date", "issued", "payment date", "transaction date", "date of issue", "date paid"];
    private static readonly string[] PartyLabels = ["supplier", "from", "contractor", "company", "issued by", "vendor", "seller", "payee", "beneficiary", "recipient", "paid to", "architect", "consultant", "sender"];
    private static readonly string[] WorkPackageLabels = ["work package", "package", "scope of work", "scope", "description of works", "re", "project description"];
    private static readonly string[] SiteLabels = ["site address", "site", "project", "property", "address", "location"];

    /// <summary>Extracts the fields for a document proposed as <paramref name="category"/>.</summary>
    public static IReadOnlyList<ExtractedField> Extract(DocumentText text, TaxonomyCategory category, IReadOnlyList<TaxonomyCategory> allCategories, string language)
    {
        var lines = text.Lines.ToList();
        const string source = "text";
        var fields = new List<ExtractedField>();

        fields.AddRange(FindAmountAndCurrency(lines, source));
        AddIfFound(fields, FindDate(lines, source) ?? PhotoFields(text.Image).FirstOrDefault(field => field.Name == FieldNames.DocumentDate));
        AddIfFound(fields, FindParty(lines, source));
        AddIfFound(fields, FindSubject(lines, source, category, allCategories));
        AddIfFound(fields, FindLabelled(FieldNames.WorkPackage, WorkPackageLabels, lines, source, 0.6));
        AddIfFound(fields, FindSiteReference(lines, source));
        AddIfFound(fields, PhotoFields(text.Image).FirstOrDefault(field => field.Name == FieldNames.Location));
        fields.AddRange(LanguageField(language));
        fields.AddRange(FindConfiguredFields(lines, source, category, fields));
        return fields;
    }

    private static void AddIfFound(List<ExtractedField> fields, ExtractedField? field)
    {
        if (field is not null)
        {
            fields.Add(field);
        }
    }

    /// <summary>
    /// Prefers an amount on a line labelled total, amount due or amount paid (the largest such amount); otherwise the
    /// largest amount with a currency anywhere, with lower confidence.
    /// </summary>
    private static IEnumerable<ExtractedField> FindAmountAndCurrency(List<(int Page, int LineNumber, TextLine Line)> lines, string source)
    {
        var candidates = lines
            .SelectMany(line => MoneyFinder.FindWithCurrency(line.Line.Text).Select(money => (line, money, Labelled: LabelledValueFinder.ContainsAny(line.Line.Text, TotalWords))))
            .ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var labelled = candidates.Where(candidate => candidate.Labelled).ToList();
        var best = (labelled.Count > 0 ? labelled : candidates).MaxBy(candidate => candidate.money.Amount);
        var confidence = (labelled.Count > 0 ? 0.9 : 0.6) * best.line.Line.Confidence;
        var evidence = Evidence(source, best.line);
        return
        [
            new ExtractedField(FieldNames.Amount, MoneyFinder.Format(best.money.Amount), confidence, evidence, FieldMethods.Regex),
            new ExtractedField(FieldNames.Currency, best.money.Currency!, 0.95 * best.line.Line.Confidence, evidence, FieldMethods.Regex),
        ];
    }

    /// <summary>A date on a line labelled "Date", "Issued", ...; otherwise the first date in the document.</summary>
    private static ExtractedField? FindDate(List<(int Page, int LineNumber, TextLine Line)> lines, string source)
    {
        var dated = lines.SelectMany(line => DateFinder.FindAll(line.Line.Text).Select(date => (line, date))).ToList();
        if (dated.Count == 0)
        {
            return null;
        }

        var labelled = dated.FirstOrDefault(candidate => LabelledValueFinder.ContainsAny(candidate.line.Line.Text, DateLabels));
        var best = labelled.line.Line is not null ? labelled : dated[0];
        var confidence = (labelled.line.Line is not null ? 0.9 : 0.6) * (best.date.Ambiguous ? 0.85 : 1) * best.line.Line.Confidence;
        var evidence = Evidence(source, best.line, best.date.Ambiguous ? " (read as day-first)" : string.Empty);
        return new ExtractedField(FieldNames.DocumentDate, DateFinder.Format(best.date.Date), confidence, evidence, FieldMethods.Regex);
    }

    /// <summary>The date a photo was taken and where, from its EXIF data. Empty for anything that is not a camera photo.</summary>
    public static IEnumerable<ExtractedField> PhotoFields(ImageFacts? image)
    {
        if (image?.TakenAt is { } takenAt)
        {
            var date = DateOnly.FromDateTime(takenAt.UtcDateTime);
            yield return new ExtractedField(FieldNames.DocumentDate, DateFinder.Format(date), 0.8, new FieldEvidence("exif", Snippet: "Date the photo was taken"), FieldMethods.Exif);
        }

        if (image?.Latitude is { } latitude && image.Longitude is { } longitude)
        {
            var value = string.Create(CultureInfo.InvariantCulture, $"{latitude:0.000000}, {longitude:0.000000}");
            yield return new ExtractedField(FieldNames.Location, value, 0.9, new FieldEvidence("exif", Snippet: "GPS position of the photo"), FieldMethods.Exif);
        }
    }

    /// <summary>A name after "Supplier:", "From:", ...; otherwise a company name ending in Ltd, (Pty) Ltd, Supplies, Construction, ...</summary>
    private static ExtractedField? FindParty(List<(int Page, int LineNumber, TextLine Line)> lines, string source)
    {
        var labelled = FindLabelled(FieldNames.Party, PartyLabels, lines, source, 0.85);
        if (labelled is not null)
        {
            return labelled;
        }

        foreach (var line in lines)
        {
            var company = CompanyName().Match(line.Line.Text);
            if (company.Success)
            {
                return new ExtractedField(FieldNames.Party, company.Value.Trim(), 0.6 * line.Line.Confidence, Evidence(source, line), FieldMethods.Regex);
            }
        }

        return null;
    }

    /// <summary>
    /// The subject chip mentioned most often, from the proposed category's chips; if none, from any category's chips,
    /// with lower confidence. "Other" is never a subject.
    /// </summary>
    private static ExtractedField? FindSubject(List<(int Page, int LineNumber, TextLine Line)> lines, string source, TaxonomyCategory category, IReadOnlyList<TaxonomyCategory> allCategories)
    {
        return FindChip(lines, source, category.SubjectChips, 0.7)
            ?? FindChip(lines, source, allCategories.SelectMany(other => other.SubjectChips).Distinct().ToList(), 0.5);
    }

    private static ExtractedField? FindChip(List<(int Page, int LineNumber, TextLine Line)> lines, string source, IReadOnlyList<string> chips, double methodConfidence)
    {
        var allText = string.Join('\n', lines.Select(line => line.Line.Text)).ToLowerInvariant();
        var best = chips
            .Where(chip => !chip.Equals("Other", StringComparison.OrdinalIgnoreCase))
            .Select(chip => (Chip: chip, Count: RulesClassifier.CountWord(allText, chip.ToLowerInvariant())))
            .Where(chip => chip.Count > 0)
            .OrderByDescending(chip => chip.Count)
            .FirstOrDefault();
        if (best.Chip is null)
        {
            return null;
        }

        var line = lines.First(candidate => LabelledValueFinder.ContainsAny(candidate.Line.Text, [best.Chip]));
        return new ExtractedField(FieldNames.Subject, best.Chip, methodConfidence * line.Line.Confidence, Evidence(source, line), FieldMethods.Keyword);
    }

    /// <summary>An erf, stand, plot or portion number; otherwise the value after "Site:", "Project:", "Property:", ...</summary>
    private static ExtractedField? FindSiteReference(List<(int Page, int LineNumber, TextLine Line)> lines, string source)
    {
        foreach (var line in lines)
        {
            var reference = PropertyReference().Match(line.Line.Text);
            if (reference.Success)
            {
                return new ExtractedField(FieldNames.SiteReference, reference.Value, 0.75 * line.Line.Confidence, Evidence(source, line), FieldMethods.Regex);
            }
        }

        return FindLabelled(FieldNames.SiteReference, SiteLabels, lines, source, 0.6);
    }

    /// <summary>The language as a field, or nothing when it could not be told.</summary>
    public static IEnumerable<ExtractedField> LanguageField(string language)
    {
        if (language != LanguageDetector.Undetermined)
        {
            yield return new ExtractedField(FieldNames.Language, language, 0.8, new FieldEvidence("text", Snippet: "Language of the text"), FieldMethods.Keyword);
        }
    }

    /// <summary>The category's own fields (for example a payslip's gross pay), found by their labels and read by their type.</summary>
    private static IEnumerable<ExtractedField> FindConfiguredFields(List<(int Page, int LineNumber, TextLine Line)> lines, string source, TaxonomyCategory category, List<ExtractedField> alreadyFound)
    {
        foreach (var definition in category.FieldsToSave.Where(definition => definition.Labels.Count > 0))
        {
            if (alreadyFound.Any(field => field.Name == definition.Name))
            {
                continue;
            }

            foreach (var line in lines)
            {
                var raw = LabelledValueFinder.ValueAfterLabel(line.Line.Text, definition.Labels);
                var value = raw is null ? null : ReadAs(definition.Type, raw);
                if (value is not null)
                {
                    yield return new ExtractedField(definition.Name, value, 0.8 * line.Line.Confidence, Evidence(source, line), FieldMethods.Regex);
                    break;
                }
            }
        }
    }

    /// <summary>Reads a raw value as its field type; null when it is not that kind of value.</summary>
    public static string? ReadAs(FieldType type, string raw)
    {
        switch (type)
        {
            case FieldType.Money:
                var money = MoneyFinder.FindFirst(raw);
                return money is null ? null : money.Currency is null ? MoneyFinder.Format(money.Amount) : $"{money.Currency} {MoneyFinder.Format(money.Amount)}";
            case FieldType.Date:
                var dates = DateFinder.FindAll(raw);
                return dates.Count == 0 ? null : DateFinder.Format(dates[0].Date);
            case FieldType.Number:
                var number = MoneyFinder.FindFirst(raw);
                return number is null ? null : number.Amount.ToString(CultureInfo.InvariantCulture);
            default:
                return Truncate(raw, MaxValueLength);
        }
    }

    private static ExtractedField? FindLabelled(string name, string[] labels, List<(int Page, int LineNumber, TextLine Line)> lines, string source, double methodConfidence)
    {
        foreach (var line in lines)
        {
            var value = LabelledValueFinder.ValueAfterLabel(line.Line.Text, labels, separatorRequired: true);
            if (value is not null && value.Any(char.IsLetter))
            {
                return new ExtractedField(name, Truncate(value, MaxValueLength), methodConfidence * line.Line.Confidence, Evidence(source, line), FieldMethods.Regex);
            }
        }

        return null;
    }

    private static FieldEvidence Evidence(string source, (int Page, int LineNumber, TextLine Line) line, string note = "")
    {
        return new FieldEvidence(source, line.Page, line.LineNumber, Truncate(line.Line.Text.Trim(), MaxSnippetLength) + note);
    }

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];

    // "ABC Supplies", "Mokoena Builders (Pty) Ltd", "Kigali Hardware Ltd": capitalised words ending in a company word.
    [GeneratedRegex(@"\b[A-Z][\w&'.\-]*(?:\s+[A-Z][\w&'.\-]*){0,4}\s+(?:\(Pty\)\s*Ltd|Pty\s+Ltd|Ltd|Limited|LLC|Inc|CC|Construction|Constructions|Supplies|Builders|Contractors|Architects|Engineers|Holdings|Hardware|Group)\b\.?", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex CompanyName();

    [GeneratedRegex(@"\b(?:Erf|Stand|Plot|Portion|Parcel|UPI)\s+(?:No\.?\s*)?[\d/\-]+\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex PropertyReference();
}
