using Microsoft.Extensions.Logging;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>
/// Reads what TUMIIA can read itself, by the file's real format: typed text from PDFs, Word, Excel, CSV and text
/// files, DXF drawing text, DWG headers and photo EXIF data. It never fails a document: a damaged file or an old
/// .doc gives no text and a plain warning.
/// </summary>
public sealed class DocumentTextReader(ILogger<DocumentTextReader> logger) : ITextReader
{
    /// <inheritdoc />
    public Task<DocumentText> ReadAsync(byte[] content, FileFormat format, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(ReadByFormat(content, format));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read a {Format} file", format.Format);
            return Task.FromResult(DocumentText.Unreadable("The file could not be read. It may be damaged."));
        }
    }

    private static DocumentText ReadByFormat(byte[] content, FileFormat format)
    {
        return format.Format switch
        {
            DetectedFormat.Pdf => PdfTextReader.Read(content),
            DetectedFormat.Jpeg or DetectedFormat.Png or DetectedFormat.Tiff or DetectedFormat.WebP or DetectedFormat.Heic => ImageTextReader.Read(content),
            DetectedFormat.Docx => WordTextReader.Read(content),
            DetectedFormat.Xlsx or DetectedFormat.Xls => SpreadsheetTextReader.Read(content),
            DetectedFormat.Text or DetectedFormat.Csv => PlainTextReader.Read(content),
            DetectedFormat.Dxf => DxfTextReader.Read(content),
            DetectedFormat.Dwg => DwgHeaderReader.Read(content),
            DetectedFormat.Doc => DocumentText.Unreadable("Old Word (.doc) files can't be read yet. Saving it as .docx or PDF gives a better result."),
            _ => DocumentText.Unreadable("TUMIIA could not tell what kind of file this is."),
        };
    }
}
