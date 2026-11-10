using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>Reads Word (.docx) files: every paragraph, including those in tables, is one line.</summary>
public static class WordTextReader
{
    /// <summary>Reads the document's body text.</summary>
    public static DocumentText Read(byte[] content)
    {
        using var document = WordprocessingDocument.Open(new MemoryStream(content), isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return DocumentText.Unreadable("The Word file has no text.");
        }

        var lines = body.Descendants<Paragraph>()
            .Select(paragraph => paragraph.InnerText.Trim())
            .Where(text => text.Length > 0)
            .Select(text => new TextLine(text, 1.0))
            .ToList();
        return new DocumentText { Pages = [new TextPage(1, lines)] };
    }
}
