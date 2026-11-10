using Tumiia.Domain.Analysis;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>
/// Reads the text layer of a PDF, line by line. A scanned PDF has little or none; the hosted AI reads its pages
/// instead. The text is still useful for the keyword rules and for checking the AI's quoted evidence.
/// </summary>
public static class PdfTextReader
{
    /// <summary>The text layer of every page, or a warning when the PDF cannot be opened.</summary>
    public static DocumentText Read(byte[] content)
    {
        try
        {
            using var document = PdfDocument.Open(content);
            return new DocumentText { Pages = document.GetPages().Select(page => new TextPage(page.Number, LinesOf(page))).ToList() };
        }
        catch (Exception exception) when (exception is PdfDocumentFormatException or PdfDocumentEncryptedException or InvalidOperationException)
        {
            return DocumentText.Unreadable("The PDF could not be opened. It may be damaged or password-protected.");
        }
    }

    // Words whose baselines are within half a letter's height of each other are on the same line; lines go top to
    // bottom and words left to right. PDF coordinates start at the bottom, so a higher Bottom is higher on the page.
    private static List<TextLine> LinesOf(Page page)
    {
        var lines = new List<List<Word>>();
        foreach (var word in page.GetWords().OrderByDescending(word => word.BoundingBox.Bottom).ThenBy(word => word.BoundingBox.Left))
        {
            var current = lines.Count > 0 ? lines[^1] : null;
            var tolerance = Math.Max(1, word.BoundingBox.Height / 2);
            if (current is null || Math.Abs(current[0].BoundingBox.Bottom - word.BoundingBox.Bottom) > tolerance)
            {
                lines.Add([word]);
            }
            else
            {
                current.Add(word);
            }
        }

        return lines
            .Select(line => string.Join(' ', line.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text)))
            .Where(text => text.Length > 0)
            .Select(text => new TextLine(text, 1.0))
            .ToList();
    }
}
