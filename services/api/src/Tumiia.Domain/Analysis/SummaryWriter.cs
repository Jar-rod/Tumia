using System.Globalization;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Writes the one-line plain-language summary from a template (never free-generated text), for example
/// "Quote from ABC Supplies for Bricks — R18,450.00, 12 Oct 2026." Only facts that were found are mentioned.
/// </summary>
public static class SummaryWriter
{
    private static readonly Dictionary<string, string> Symbols = new()
    {
        ["ZAR"] = "R",
        ["USD"] = "$",
        ["EUR"] = "€",
        ["GBP"] = "£",
        ["ZWG"] = "ZiG ",
        ["RWF"] = "RWF ",
        ["KES"] = "KSh ",
    };

    /// <summary>The summary for a document proposed as <paramref name="categoryName"/> with these fields.</summary>
    public static string Write(string categoryName, IReadOnlyList<ExtractedField> fields)
    {
        var summary = categoryName;
        var party = Value(fields, FieldNames.Party);
        var subject = Value(fields, FieldNames.Subject);
        var money = Money(fields);
        var date = Value(fields, FieldNames.DocumentDate);

        if (party is not null)
        {
            summary += $" from {party}";
        }

        if (subject is not null)
        {
            summary += $" for {subject}";
        }

        if (money is not null)
        {
            summary += $" — {money}";
        }

        if (date is not null)
        {
            summary += (money is null ? " — " : ", ") + ReadableDate(date);
        }

        return summary + ".";
    }

    private static string? Value(IReadOnlyList<ExtractedField> fields, string name)
    {
        return fields.FirstOrDefault(field => field.Name == name)?.Value;
    }

    private static string? Money(IReadOnlyList<ExtractedField> fields)
    {
        var amount = Value(fields, FieldNames.Amount);
        if (amount is null || !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var currency = Value(fields, FieldNames.Currency);
        var symbol = currency is null ? string.Empty : Symbols.GetValueOrDefault(currency, currency + " ");
        return symbol + value.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }

    private static string ReadableDate(string isoDate)
    {
        return DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
            : isoDate;
    }
}
