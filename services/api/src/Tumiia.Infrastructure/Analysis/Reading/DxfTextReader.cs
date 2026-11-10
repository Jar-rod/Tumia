using System.Text;
using System.Text.RegularExpressions;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>
/// Reads AutoCAD DXF drawings, which are plain text made of "group code / value" pairs. Takes the text on the drawing
/// (TEXT and MTEXT, which include the title block), the layer names and the drawing units. No CAD library is needed.
/// </summary>
public static partial class DxfTextReader
{
    private static readonly Dictionary<string, string> Units = new()
    {
        ["1"] = "inches",
        ["2"] = "feet",
        ["4"] = "millimetres",
        ["5"] = "centimetres",
        ["6"] = "metres",
    };

    /// <summary>Reads the drawing's text as one page, and its units and layers as file header facts.</summary>
    public static DocumentText Read(byte[] content)
    {
        var pairs = Pairs(Encoding.UTF8.GetString(content));
        var lines = new List<TextLine>();
        var layers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        string? units = null;
        string? entity = null;
        string? table = null;

        for (var index = 0; index < pairs.Count; index++)
        {
            var (code, value) = pairs[index];
            switch (code)
            {
                case "0":
                    entity = value;
                    break;
                case "2" when entity == "TABLE":
                    table = value;
                    break;
                case "2" when entity == "LAYER" && table == "LAYER":
                    layers.Add(value);
                    break;
                case "9" when value == "$INSUNITS" && index + 1 < pairs.Count:
                    units = Units.GetValueOrDefault(pairs[index + 1].Value);
                    break;
                case "1" or "3" when entity is "TEXT" or "MTEXT" or "ATTRIB":
                    AddText(lines, value);
                    break;
            }
        }

        var facts = new List<string> { "AutoCAD DXF drawing" };
        if (units is not null)
        {
            facts.Add($"Drawing units: {units}");
        }

        if (layers.Count > 0)
        {
            facts.Add("Layers: " + string.Join(", ", layers.Take(50)));
        }

        return new DocumentText { Pages = [new TextPage(1, lines)], FileHeaderFacts = facts };
    }

    private static List<(string Code, string Value)> Pairs(string text)
    {
        var rows = text.Split('\n').Select(row => row.TrimEnd('\r')).ToList();
        var pairs = new List<(string, string)>();
        for (var index = 0; index + 1 < rows.Count; index += 2)
        {
            pairs.Add((rows[index].Trim(), rows[index + 1].Trim()));
        }

        return pairs;
    }

    // MTEXT carries formatting codes such as \P (new paragraph) and {\fArial|b0;...}; keep only the words.
    private static void AddText(List<TextLine> lines, string value)
    {
        var plain = MtextCodes().Replace(value.Replace("\\P", "\n"), string.Empty).Replace("{", string.Empty).Replace("}", string.Empty);
        foreach (var part in plain.Split('\n').Select(part => part.Trim()).Where(part => part.Length > 0))
        {
            lines.Add(new TextLine(part, 1.0));
        }
    }

    [GeneratedRegex(@"\\[A-Za-z][^;\\{}]*;|\\[A-Za-z]", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex MtextCodes();
}
