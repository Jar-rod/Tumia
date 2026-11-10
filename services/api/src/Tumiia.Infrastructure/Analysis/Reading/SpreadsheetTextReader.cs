using System.Globalization;
using System.Text;
using ExcelDataReader;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>Reads Excel files (.xlsx and old .xls). Each sheet is a page: its name, then one line per row with the cells separated by " | ".</summary>
public static class SpreadsheetTextReader
{
    private const int MaxRowsPerSheet = 2000;

    static SpreadsheetTextReader()
    {
        // Old .xls files use Windows code pages, which .NET only knows once this provider is registered.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>Reads every sheet, up to 2000 rows each.</summary>
    public static DocumentText Read(byte[] content)
    {
        using var reader = ExcelReaderFactory.CreateReader(new MemoryStream(content));
        var pages = new List<TextPage>();
        do
        {
            var lines = new List<TextLine> { new($"Sheet: {reader.Name}", 1.0) };
            var rows = 0;
            while (reader.Read() && rows++ < MaxRowsPerSheet)
            {
                var cells = Enumerable.Range(0, reader.FieldCount)
                    .Select(column => Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture)?.Trim())
                    .Where(cell => !string.IsNullOrEmpty(cell));
                var line = string.Join(" | ", cells);
                if (line.Length > 0)
                {
                    lines.Add(new TextLine(line, 1.0));
                }
            }

            pages.Add(new TextPage(pages.Count + 1, lines));
        }
        while (reader.NextResult());

        return new DocumentText { Pages = pages };
    }
}
