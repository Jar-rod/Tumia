using System.Text;
using System.Text.Unicode;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Works out a file's real format from its first bytes ("magic numbers"), because a file's name can be wrong.
/// Office files (.docx, .xlsx) are ZIP files, so they are told apart by the folder names inside the ZIP.
/// </summary>
public static class FormatDetector
{
    private static readonly Dictionary<DetectedFormat, (string ContentType, string[] Extensions)> Formats = new()
    {
        [DetectedFormat.Pdf] = ("application/pdf", [".pdf"]),
        [DetectedFormat.Jpeg] = ("image/jpeg", [".jpg", ".jpeg"]),
        [DetectedFormat.Png] = ("image/png", [".png"]),
        [DetectedFormat.Tiff] = ("image/tiff", [".tif", ".tiff"]),
        [DetectedFormat.WebP] = ("image/webp", [".webp"]),
        [DetectedFormat.Heic] = ("image/heic", [".heic", ".heif"]),
        [DetectedFormat.Docx] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", [".docx"]),
        [DetectedFormat.Xlsx] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [".xlsx"]),
        [DetectedFormat.Doc] = ("application/msword", [".doc"]),
        [DetectedFormat.Xls] = ("application/vnd.ms-excel", [".xls"]),
        [DetectedFormat.Csv] = ("text/csv", [".csv"]),
        [DetectedFormat.Text] = ("text/plain", [".txt"]),
        [DetectedFormat.Dxf] = ("image/vnd.dxf", [".dxf"]),
        [DetectedFormat.Dwg] = ("image/vnd.dwg", [".dwg"]),
        [DetectedFormat.Unknown] = ("application/octet-stream", []),
    };

    /// <summary>Detects the format of <paramref name="content"/>, and checks it against the extension of <paramref name="fileName"/>.</summary>
    public static FileFormat Detect(ReadOnlySpan<byte> content, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var format = DetectFromBytes(content, extension);
        var (contentType, extensions) = Formats[format];
        return new FileFormat(format, contentType, extensions.Contains(extension));
    }

    private static DetectedFormat DetectFromBytes(ReadOnlySpan<byte> content, string extension)
    {
        if (StartsWith(content, "%PDF"u8))
        {
            return DetectedFormat.Pdf;
        }

        if (StartsWith(content, [0xFF, 0xD8, 0xFF]))
        {
            return DetectedFormat.Jpeg;
        }

        if (StartsWith(content, [0x89, 0x50, 0x4E, 0x47]))
        {
            return DetectedFormat.Png;
        }

        if (StartsWith(content, "II*\0"u8) || StartsWith(content, "MM\0*"u8))
        {
            return DetectedFormat.Tiff;
        }

        if (StartsWith(content, "RIFF"u8) && content.Length >= 12 && content[8..12].SequenceEqual("WEBP"u8))
        {
            return DetectedFormat.WebP;
        }

        if (IsHeic(content))
        {
            return DetectedFormat.Heic;
        }

        if (StartsWith(content, "PK\u0003\u0004"u8))
        {
            return DetectOfficeZip(content);
        }

        if (StartsWith(content, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]))
        {
            // Old Word and Excel files share one container format; only the name tells them apart cheaply.
            return extension == ".xls" ? DetectedFormat.Xls : DetectedFormat.Doc;
        }

        // Every AutoCAD drawing since release 2.5 starts with its version, "AC1009" to "AC1032".
        if (StartsWith(content, "AC10"u8))
        {
            return DetectedFormat.Dwg;
        }

        return DetectText(content, extension);
    }

    // HEIC photos (iPhone) are ISO media files: "ftyp" at byte 4, then a brand such as "heic" or "mif1".
    private static bool IsHeic(ReadOnlySpan<byte> content)
    {
        if (content.Length < 12 || !content[4..8].SequenceEqual("ftyp"u8))
        {
            return false;
        }

        var brand = Encoding.ASCII.GetString(content[8..12]);
        return brand is "heic" or "heix" or "heim" or "heis" or "hevc" or "hevx" or "mif1" or "msf1";
    }

    // A .docx keeps its text under "word/", an .xlsx under "xl/". Those names are stored as plain text in the ZIP.
    private static DetectedFormat DetectOfficeZip(ReadOnlySpan<byte> content)
    {
        if (content.IndexOf("word/"u8) >= 0)
        {
            return DetectedFormat.Docx;
        }

        return content.IndexOf("xl/"u8) >= 0 ? DetectedFormat.Xlsx : DetectedFormat.Unknown;
    }

    private static DetectedFormat DetectText(ReadOnlySpan<byte> content, string extension)
    {
        var sample = content[..Math.Min(content.Length, 8192)];
        if (sample.Length == 0 || sample.IndexOf((byte)0) >= 0 || !Utf8.IsValid(TrimPartialCharacter(sample)))
        {
            return DetectedFormat.Unknown;
        }

        if (IsDxf(sample))
        {
            return DetectedFormat.Dxf;
        }

        return extension == ".csv" ? DetectedFormat.Csv : DetectedFormat.Text;
    }

    // A DXF file is text that starts with "0 / SECTION" pairs and names sections such as HEADER or ENTITIES.
    private static bool IsDxf(ReadOnlySpan<byte> sample)
    {
        var text = Encoding.UTF8.GetString(sample);
        var lines = text.Split('\n', 3);
        return lines.Length >= 2 && lines[0].Trim() == "0" && lines[1].Trim() == "SECTION";
    }

    // The 8 KB sample may cut a multi-byte letter in half; drop up to 3 trailing bytes of an unfinished letter.
    private static ReadOnlySpan<byte> TrimPartialCharacter(ReadOnlySpan<byte> sample)
    {
        for (var cut = 0; cut <= 3 && cut < sample.Length; cut++)
        {
            if (Utf8.IsValid(sample[..^cut]))
            {
                return sample[..^cut];
            }
        }

        return sample;
    }

    private static bool StartsWith(ReadOnlySpan<byte> content, ReadOnlySpan<byte> prefix)
    {
        return content.StartsWith(prefix);
    }
}
