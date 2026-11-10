using System.Text;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>
/// DWG drawings are a closed binary format, so only their header is read: the AutoCAD version. The document is then
/// classified from its format and file name, and its confidence is capped below 0.85.
/// </summary>
public static class DwgHeaderReader
{
    private static readonly Dictionary<string, string> Versions = new()
    {
        ["AC1009"] = "R11/R12",
        ["AC1012"] = "R13",
        ["AC1014"] = "R14",
        ["AC1015"] = "2000",
        ["AC1018"] = "2004",
        ["AC1021"] = "2007",
        ["AC1024"] = "2010",
        ["AC1027"] = "2013",
        ["AC1032"] = "2018",
    };

    /// <summary>No text, the version as a file header fact, and a warning that the content cannot be read.</summary>
    public static DocumentText Read(byte[] content)
    {
        var code = Encoding.ASCII.GetString(content, 0, Math.Min(6, content.Length));
        var version = Versions.GetValueOrDefault(code, code);
        return new DocumentText
        {
            FileHeaderFacts = [$"AutoCAD DWG drawing, version {version}"],
            Warnings = ["DWG drawings can't be read inside, so this was judged on its format and file name."],
        };
    }
}
