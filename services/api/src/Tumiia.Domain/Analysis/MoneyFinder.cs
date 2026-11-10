using System.Globalization;
using System.Text.RegularExpressions;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Finds amounts of money in text, such as "R18,450.00", "ZAR 2 000", "€2.000,50", "RWF 150,000" or "KSh 3,500".
/// Knows the rand, US dollar, euro, pound, Zimbabwe Gold (ZiG), Rwandan franc and Kenyan shilling.
/// </summary>
public static partial class MoneyFinder
{
    private static readonly Dictionary<string, string> CurrencyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["R"] = "ZAR",
        ["ZAR"] = "ZAR",
        ["$"] = "USD",
        ["US$"] = "USD",
        ["USD"] = "USD",
        ["€"] = "EUR",
        ["EUR"] = "EUR",
        ["£"] = "GBP",
        ["GBP"] = "GBP",
        ["ZiG"] = "ZWG",
        ["ZWG"] = "ZWG",
        ["RWF"] = "RWF",
        ["FRw"] = "RWF",
        ["RF"] = "RWF",
        ["KES"] = "KES",
        ["KSh"] = "KES",
        ["Ksh"] = "KES",
    };

    /// <summary>Every amount that has a currency in <paramref name="line"/>, in order.</summary>
    public static IReadOnlyList<MoneyAmount> FindWithCurrency(string line)
    {
        var found = new List<MoneyAmount>();
        foreach (Match match in CurrencyAmount().Matches(line))
        {
            var symbol = match.Groups["before"].Success ? match.Groups["before"].Value : match.Groups["after"].Value;
            if (TryParseNumber(match.Groups["number"].Value, out var amount))
            {
                found.Add(new MoneyAmount(amount, CurrencyCodes[symbol]));
            }
        }

        return found;
    }

    /// <summary>The first amount in <paramref name="text"/>, with its currency if one is shown. Used for labelled fields.</summary>
    public static MoneyAmount? FindFirst(string text)
    {
        var withCurrency = FindWithCurrency(text);
        if (withCurrency.Count > 0)
        {
            return withCurrency[0];
        }

        var bare = BareNumber().Match(text);
        return bare.Success && TryParseNumber(bare.Value, out var amount) ? new MoneyAmount(amount, null) : null;
    }

    /// <summary>
    /// Reads "18,450.00", "18 450,00", "18.450,00" or "2000". The last separator is the decimal mark only when exactly
    /// one or two digits follow it; otherwise every separator groups thousands.
    /// </summary>
    public static bool TryParseNumber(string text, out decimal amount)
    {
        var cleaned = text.Replace(" ", string.Empty).Replace(" ", string.Empty).Replace("'", string.Empty);
        var lastSeparator = cleaned.LastIndexOfAny([',', '.']);
        var decimals = lastSeparator < 0 ? 0 : cleaned.Length - lastSeparator - 1;

        string normalised;
        if (lastSeparator >= 0 && decimals is 1 or 2)
        {
            var whole = cleaned[..lastSeparator].Replace(",", string.Empty).Replace(".", string.Empty);
            normalised = $"{whole}.{cleaned[(lastSeparator + 1)..]}";
        }
        else
        {
            normalised = cleaned.Replace(",", string.Empty).Replace(".", string.Empty);
        }

        return decimal.TryParse(normalised, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }

    /// <summary>The amount written the way people read it: "18450.00".</summary>
    public static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    // A currency before ("R 18,450.00"; not the "R" at the end of a word such as "FOR"), "US$2,000") or after ("150,000 RWF") a number with optional separators.
    [GeneratedRegex(@"(?:(?<![A-Za-z])(?<before>US\$|ZAR|USD|EUR|GBP|ZWG|ZiG|RWF|FRw|KES|KSh|Ksh|RF|R|\$|€|£)\s?(?<number>\d{1,3}(?:[ ,. ']\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?)(?![\d])|(?<number>\d{1,3}(?:[ ,. ']\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?)\s?(?<after>ZAR|USD|EUR|GBP|ZWG|ZiG|RWF|FRw|KES)\b)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex CurrencyAmount();

    [GeneratedRegex(@"\d{1,3}(?:[ ,.]\d{3})+(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex BareNumber();
}
