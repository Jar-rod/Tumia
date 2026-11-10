namespace Tumiia.Domain.Analysis;

/// <summary>
/// Guesses a text's language by counting very common words ("the", "and", "le", "na", ...). It is simple, but enough
/// to record the language and to warn when a document is not in English (the OCR only reads English well).
/// Returns an ISO 639-1 code, or "und" (undetermined) when there is too little text to tell.
/// </summary>
public static class LanguageDetector
{
    /// <summary>The code used when the language cannot be told.</summary>
    public const string Undetermined = "und";

    private const int MinimumHits = 3;

    private static readonly Dictionary<string, HashSet<string>> CommonWords = new()
    {
        ["en"] = ["the", "and", "for", "of", "to", "is", "with", "this", "that", "on", "are", "be", "by", "from", "please", "total", "date"],
        ["fr"] = ["le", "la", "les", "et", "des", "du", "pour", "est", "une", "dans", "avec", "sur", "au", "ce", "montant"],
        ["pt"] = ["o", "os", "da", "do", "das", "dos", "e", "para", "com", "uma", "não", "em", "que", "valor"],
        ["af"] = ["die", "en", "van", "is", "het", "vir", "nie", "dit", "met", "op", "te", "aan", "bedrag"],
        ["sw"] = ["na", "ya", "wa", "kwa", "za", "ni", "katika", "hii", "la", "kama", "jumla", "tarehe"],
        ["rw"] = ["mu", "ku", "na", "ni", "cyangwa", "kandi", "kugira", "ngo", "iyi", "uyu", "ari", "amafaranga"],
    };

    /// <summary>The most likely language of <paramref name="text"/>, or <see cref="Undetermined"/>.</summary>
    public static string Detect(string text)
    {
        var words = text.ToLowerInvariant()
            .Split((char[])[' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries);

        var best = CommonWords
            .Select(language => (Code: language.Key, Hits: words.Count(language.Value.Contains)))
            .OrderByDescending(language => language.Hits)
            .First();

        return best.Hits >= MinimumHits ? best.Code : Undetermined;
    }
}
