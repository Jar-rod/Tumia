namespace Tumiia.Domain.Analysis;

/// <summary>
/// Everything TUMIIA read from a file itself: typed text by page and line, photo facts, CAD facts, and any problems met
/// while reading. Scans and photos have no text here; the hosted AI reads those.
/// </summary>
public sealed record DocumentText
{
    /// <summary>The pages read, in order. Empty when nothing could be read.</summary>
    public IReadOnlyList<TextPage> Pages { get; init; } = [];

    /// <summary>Problems met while reading, in plain words. They are kept with the analysis.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>EXIF facts, for photos.</summary>
    public ImageFacts? Image { get; init; }

    /// <summary>Extra facts from a CAD file (version, layers, units), shown as evidence.</summary>
    public IReadOnlyList<string> FileHeaderFacts { get; init; } = [];

    /// <summary>All the text, one line per line, for classification.</summary>
    public string AllText => string.Join('\n', Lines.Select(line => line.Line.Text));

    /// <summary>Every line with its page number and its 1-based line number on that page.</summary>
    public IEnumerable<(int Page, int LineNumber, TextLine Line)> Lines =>
        Pages.SelectMany(page => page.Lines.Select((line, index) => (page.Number, index + 1, line)));

    /// <summary>How many letters and digits were read; a rough measure of how much text there is.</summary>
    public int CharacterCount => Lines.Sum(line => line.Line.Text.Count(char.IsLetterOrDigit));

    /// <summary>The average confidence across lines, weighted by line length. 0 when there is no text.</summary>
    public double AverageConfidence
    {
        get
        {
            var weighted = Lines.Select(line => (Weight: line.Line.Text.Length, line.Line.Confidence)).Where(line => line.Weight > 0).ToList();
            var totalWeight = weighted.Sum(line => line.Weight);
            return totalWeight == 0 ? 0 : weighted.Sum(line => line.Weight * line.Confidence) / totalWeight;
        }
    }

    /// <summary>A result with no text and one warning, for files that cannot be read.</summary>
    public static DocumentText Unreadable(string warning) => new() { Warnings = [warning] };
}
