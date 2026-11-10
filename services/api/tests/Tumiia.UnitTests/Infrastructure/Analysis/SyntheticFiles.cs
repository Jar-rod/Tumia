using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace Tumiia.UnitTests.Infrastructure.Analysis;

/// <summary>Builds small synthetic PDF, Word, Excel and DXF files in memory for the reader tests.</summary>
public static class SyntheticFiles
{
    /// <summary>A PDF with a text layer: each page gets the given lines, top to bottom.</summary>
    public static byte[] Pdf(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            for (var index = 0; index < lines.Length; index++)
            {
                page.AddText(lines[index], 12, new PdfPoint(40, 780 - (index * 20)), font);
            }
        }

        return builder.Build();
    }

    /// <summary>A .docx with one paragraph per line.</summary>
    public static byte[] Docx(params string[] paragraphs)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Word.Document(new Word.Body(paragraphs.Select(text => new Word.Paragraph(new Word.Run(new Word.Text(text))))));
        }

        return stream.ToArray();
    }

    /// <summary>An .xlsx with one sheet, one row per array.</summary>
    public static byte[] Xlsx(string sheetName, params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var worksheet = workbook.AddNewPart<WorksheetPart>();
            var data = new SheetData(rows.Select(row => new Row(row.Select(cell => new Cell { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(cell)) }))));
            worksheet.Worksheet = new Worksheet(data);
            workbook.Workbook.AppendChild(new Sheets(new Sheet { Id = workbook.GetIdOfPart(worksheet), SheetId = 1, Name = sheetName }));
        }

        return stream.ToArray();
    }

    /// <summary>A small DXF with two layers, units in millimetres, a TEXT and an MTEXT title block.</summary>
    public static byte[] Dxf()
    {
        var pairs = new[]
        {
            "0", "SECTION", "2", "HEADER", "9", "$INSUNITS", "70", "4", "0", "ENDSEC",
            "0", "SECTION", "2", "TABLES", "0", "TABLE", "2", "LAYER",
            "0", "LAYER", "2", "A-WALL", "70", "0", "0", "LAYER", "2", "A-DOOR", "70", "0", "0", "ENDTAB", "0", "ENDSEC",
            "0", "SECTION", "2", "ENTITIES",
            "0", "TEXT", "8", "A-ANNO", "1", "GROUND FLOOR PLAN",
            "0", "MTEXT", "8", "A-ANNO", "1", @"{\fArial|b1;Scale 1:100}\PDrawing No A-101",
            "0", "ENDSEC", "0", "EOF",
        };
        return System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", pairs) + "\r\n");
    }
}
