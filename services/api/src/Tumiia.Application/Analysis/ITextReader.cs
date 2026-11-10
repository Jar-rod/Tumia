using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>Reads the text out of a file: typed text directly, scans and photos with OCR. Implemented in Infrastructure.</summary>
public interface ITextReader
{
    /// <summary>
    /// Reads <paramref name="content"/>, already detected as <paramref name="format"/>. Never throws for a file it
    /// cannot read: it returns no text and a warning, so the document goes to Needs Review.
    /// </summary>
    Task<DocumentText> ReadAsync(byte[] content, FileFormat format, CancellationToken cancellationToken);
}
