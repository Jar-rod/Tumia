using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Tumiia.Domain.Analysis;
using Tumiia.Infrastructure.Analysis.Reading;

namespace Tumiia.UnitTests.Infrastructure.Analysis;

public class TextReaderTests
{
    private readonly DocumentTextReader _reader = new(NullLogger<DocumentTextReader>.Instance);

    private Task<DocumentText> ReadAsync(byte[] content, string fileName)
    {
        return _reader.ReadAsync(content, FormatDetector.Detect(content, fileName), CancellationToken.None);
    }

    [Fact]
    public async Task ReadAsync_WhenAPdfHasATextLayer_ReadsItLineByLine()
    {
        var pdf = SyntheticFiles.Pdf(["ABC Supplies (Pty) Ltd", "QUOTATION", "Total R 18,450.00 for the bricks and cement delivered"]);

        var text = await ReadAsync(pdf, "quote.pdf");

        text.Pages.ShouldHaveSingleItem().Lines.Select(line => line.Text).ShouldBe(["ABC Supplies (Pty) Ltd", "QUOTATION", "Total R 18,450.00 for the bricks and cement delivered"]);
    }

    [Fact]
    public async Task ReadAsync_WhenAPdfHasSeveralPages_NumbersThem()
    {
        var text = await ReadAsync(SyntheticFiles.Pdf(["first page"], ["second page"]), "two.pdf");

        text.Pages.Select(page => page.Number).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task ReadAsync_WhenThePdfIsDamaged_ReturnsAWarningInsteadOfFailing()
    {
        var text = await ReadAsync(Encoding.ASCII.GetBytes("%PDF-1.7 this is not really a pdf"), "broken.pdf");

        text.Pages.ShouldBeEmpty();
        text.Warnings.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "receipt.png")]
    [InlineData(new byte[] { 0, 0, 0, 24, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63 }, "IMG_3837.heic")]
    public async Task ReadAsync_WhenTheFileIsAnImage_LeavesTheWordsToTheAiAndReportsNoProblem(byte[] content, string fileName)
    {
        var text = await ReadAsync(content, fileName);

        text.Pages.ShouldBeEmpty();
        text.Warnings.ShouldBeEmpty();
        text.Image.ShouldBeNull();
    }

    [Fact]
    public async Task ReadAsync_WhenAWordFileIsRead_TakesEachParagraphAsALine()
    {
        var text = await ReadAsync(SyntheticFiles.Docx("Letter of appointment", "", "Mokoena Builders CC is appointed."), "appointment.docx");

        text.Pages.ShouldHaveSingleItem().Lines.Select(line => line.Text).ShouldBe(["Letter of appointment", "Mokoena Builders CC is appointed."]);
    }

    [Fact]
    public async Task ReadAsync_WhenASpreadsheetIsRead_TakesTheSheetNameAndOneLinePerRow()
    {
        var xlsx = SyntheticFiles.Xlsx("Prices", ["Item", "Price"], ["Bricks", "R 2.50"], [string.Empty, string.Empty]);

        var text = await ReadAsync(xlsx, "prices.xlsx");

        text.Pages.ShouldHaveSingleItem().Lines.Select(line => line.Text).ShouldBe(["Sheet: Prices", "Item | Price", "Bricks | R 2.50"]);
    }

    [Fact]
    public async Task ReadAsync_WhenADxfIsRead_TakesItsTextLayersAndUnits()
    {
        var text = await ReadAsync(SyntheticFiles.Dxf(), "plan.dxf");

        text.Pages.ShouldHaveSingleItem().Lines.Select(line => line.Text).ShouldBe(["GROUND FLOOR PLAN", "Scale 1:100", "Drawing No A-101"]);
        text.FileHeaderFacts.ShouldBe(["AutoCAD DXF drawing", "Drawing units: millimetres", "Layers: A-DOOR, A-WALL"]);
    }

    [Fact]
    public async Task ReadAsync_WhenADwgIsRead_TakesOnlyItsVersionAndWarns()
    {
        var text = await ReadAsync(Encoding.ASCII.GetBytes("AC1032\0\0\0"), "plan.dwg");

        text.Pages.ShouldBeEmpty();
        text.FileHeaderFacts.ShouldBe(["AutoCAD DWG drawing, version 2018"]);
        text.Warnings.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ReadAsync_WhenPlainTextIsRead_SkipsEmptyLinesAndTheByteOrderMark()
    {
        var text = await ReadAsync(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("Hi Sipho\r\n\r\nPlease deliver on Monday.")).ToArray(), "note.txt");

        text.Pages.ShouldHaveSingleItem().Lines.Select(line => line.Text).ShouldBe(["Hi Sipho", "Please deliver on Monday."]);
    }

    [Fact]
    public void PlainTextReader_WhenTheBytesAreNotUtf8_ReadsThemAsWindowsText()
    {
        PlainTextReader.Read([0x43, 0x61, 0x66, 0xE9]).AllText.ShouldBe("Café");
    }

    [Theory]
    [InlineData(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0 }, "old.doc", "Old Word")]
    [InlineData(new byte[] { 0x00, 0x01, 0x02 }, "mystery.bin", "could not tell")]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x77, 0x6F, 0x72, 0x64, 0x2F }, "broken.docx", "damaged")]
    public async Task ReadAsync_WhenTheFileCannotBeRead_ExplainsWhyInPlainWords(byte[] content, string fileName, string expected)
    {
        var text = await ReadAsync(content, fileName);

        text.Pages.ShouldBeEmpty();
        text.Warnings.ShouldHaveSingleItem().ShouldContain(expected);
    }
}
