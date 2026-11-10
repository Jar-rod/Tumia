using System.Globalization;
using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Finds dates in text: "2026-10-12", "12/10/2026", "12.10.26", "12 Oct 2026", "12 October 2026" and "Oct 12, 2026".
/// Numeric dates are read day-first, as in South Africa, Rwanda and Kenya. Only years 1990 to 2100 count, so
/// numbers such as quantities are not mistaken for dates.
/// </summary>
public static partial class DateFinder
{
    private static readonly string[] MonthNames =
        ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];

    /// <summary>Every date in <paramref name="text"/>, in order.</summary>
    public static IReadOnlyList<FoundDate> FindAll(string text)
    {
        var found = new List<(int Index, FoundDate Date)>();
        foreach (Match match in IsoDate().Matches(text))
        {
            Add(found, match.Index, Number(match, "year"), Number(match, "month"), Number(match, "day"), ambiguous: false);
        }

        foreach (Match match in NumericDate().Matches(text))
        {
            var day = Number(match, "day");
            var month = Number(match, "month");
            Add(found, match.Index, FullYear(Number(match, "year")), month, day, ambiguous: day <= 12 && month <= 12 && day != month);
        }

        foreach (Match match in DayMonthNameDate().Matches(text))
        {
            Add(found, match.Index, FullYear(Number(match, "year")), MonthNumber(match.Groups["month"].Value), Number(match, "day"), ambiguous: false);
        }

        foreach (Match match in MonthNameDayDate().Matches(text))
        {
            Add(found, match.Index, FullYear(Number(match, "year")), MonthNumber(match.Groups["month"].Value), Number(match, "day"), ambiguous: false);
        }

        return found.OrderBy(date => date.Index).Select(date => date.Date).ToList();
    }

    /// <summary>The date written as yyyy-MM-dd.</summary>
    public static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static void Add(List<(int, FoundDate)> found, int index, int year, int month, int day, bool ambiguous)
    {
        if (year is < 1990 or > 2100 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return;
        }

        found.Add((index, new FoundDate(new DateOnly(year, month, day), ambiguous)));
    }

    private static int Number(Match match, string group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    // Two-digit years are taken as 20xx.
    private static int FullYear(int year) => year < 100 ? 2000 + year : year;

    private static int MonthNumber(string name) => Array.IndexOf(MonthNames, name[..3].ToLowerInvariant()) + 1;

    [GeneratedRegex(@"\b(?<year>\d{4})-(?<month>\d{1,2})-(?<day>\d{1,2})\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex IsoDate();

    [GeneratedRegex(@"\b(?<day>\d{1,2})[/.\-](?<month>\d{1,2})[/.\-](?<year>\d{4}|\d{2})\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex NumericDate();

    [GeneratedRegex(@"\b(?<day>\d{1,2})(?:st|nd|rd|th)?\s+(?<month>jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?,?\s+(?<year>\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex DayMonthNameDate();

    [GeneratedRegex(@"\b(?<month>jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\s+(?<day>\d{1,2})(?:st|nd|rd|th)?,?\s+(?<year>\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex MonthNameDayDate();
}
