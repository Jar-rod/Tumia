namespace Tumiia.Domain.Analysis;

/// <summary>What a file really is, worked out from its first bytes rather than its name.</summary>
public enum DetectedFormat
{
    Unknown,
    Pdf,
    Jpeg,
    Png,
    Tiff,
    WebP,
    Heic,
    Docx,
    Xlsx,
    Doc,
    Xls,
    Csv,
    Text,
    Dxf,
    Dwg,
}
