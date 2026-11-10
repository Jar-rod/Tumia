namespace Tumiia.Domain.Analysis;

/// <summary>The real format of a file, its content type, and whether its extension says the same thing.</summary>
/// <param name="Format">The detected format.</param>
/// <param name="ContentType">The content type that goes with the format.</param>
/// <param name="ExtensionMatches">False when, say, a PDF was named ".jpg". A mismatch lowers confidence.</param>
public sealed record FileFormat(DetectedFormat Format, string ContentType, bool ExtensionMatches)
{
    /// <summary>True for photos, scans and screenshots, which are read with OCR.</summary>
    public bool IsImage => Format is DetectedFormat.Jpeg or DetectedFormat.Png or DetectedFormat.Tiff or DetectedFormat.WebP or DetectedFormat.Heic;

    /// <summary>True for CAD drawings, which have little ordinary text.</summary>
    public bool IsCad => Format is DetectedFormat.Dxf or DetectedFormat.Dwg;
}
