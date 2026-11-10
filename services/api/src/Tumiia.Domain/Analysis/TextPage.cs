namespace Tumiia.Domain.Analysis;

/// <summary>One page (or sheet, or the whole of a plain-text file) of text read from a file.</summary>
/// <param name="Number">The page number, starting at 1.</param>
/// <param name="Lines">The page's lines, top to bottom.</param>
public sealed record TextPage(int Number, IReadOnlyList<TextLine> Lines);
