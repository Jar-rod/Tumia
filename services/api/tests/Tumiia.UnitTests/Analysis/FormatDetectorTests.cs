using System.Text;
using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class FormatDetectorTests
{
    public static TheoryData<byte[], string, DetectedFormat> KnownFiles => new()
    {
        { Encoding.ASCII.GetBytes("%PDF-1.7\n..."), "quote.pdf", DetectedFormat.Pdf },
        { [0xFF, 0xD8, 0xFF, 0xE0, 0, 0], "site.jpg", DetectedFormat.Jpeg },
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A], "screenshot.png", DetectedFormat.Png },
        { Encoding.ASCII.GetBytes("II*\0rest"), "scan.tif", DetectedFormat.Tiff },
        { Encoding.ASCII.GetBytes("RIFF1234WEBPVP8 "), "photo.webp", DetectedFormat.WebP },
        { [0, 0, 0, 24, .. Encoding.ASCII.GetBytes("ftypheic0000")], "IMG_3837.heic", DetectedFormat.Heic },
        { [.. Encoding.ASCII.GetBytes("PK\u0003\u0004"), .. Encoding.ASCII.GetBytes("....word/document.xml")], "letter.docx", DetectedFormat.Docx },
        { [.. Encoding.ASCII.GetBytes("PK\u0003\u0004"), .. Encoding.ASCII.GetBytes("....xl/workbook.xml")], "prices.xlsx", DetectedFormat.Xlsx },
        { [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0], "old.doc", DetectedFormat.Doc },
        { [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0], "old.xls", DetectedFormat.Xls },
        { Encoding.ASCII.GetBytes("AC1032\0\0binary"), "plan.dwg", DetectedFormat.Dwg },
        { Encoding.ASCII.GetBytes("  0\r\nSECTION\r\n  2\r\nHEADER\r\n"), "plan.dxf", DetectedFormat.Dxf },
        { Encoding.UTF8.GetBytes("item,qty\nbricks,100"), "list.csv", DetectedFormat.Csv },
        { Encoding.UTF8.GetBytes("Please deliver the blocks on Monday."), "note.txt", DetectedFormat.Text },
    };

    [Theory]
    [MemberData(nameof(KnownFiles))]
    public void Detect_WhenTheFileStartsWithAKnownSignature_FindsItsRealFormat(byte[] content, string fileName, DetectedFormat expected)
    {
        var format = FormatDetector.Detect(content, fileName);

        format.Format.ShouldBe(expected);
        format.ExtensionMatches.ShouldBeTrue();
    }

    [Fact]
    public void Detect_WhenAPdfIsNamedLikeAPhoto_SaysTheExtensionDoesNotMatch()
    {
        var format = FormatDetector.Detect(Encoding.ASCII.GetBytes("%PDF-1.4"), "IMG_0001.jpg");

        format.Format.ShouldBe(DetectedFormat.Pdf);
        format.ContentType.ShouldBe("application/pdf");
        format.ExtensionMatches.ShouldBeFalse();
    }

    [Fact]
    public void Detect_WhenTheBytesAreBinaryAndUnknown_SaysUnknown()
    {
        var format = FormatDetector.Detect([0x00, 0x01, 0x02, 0xFE], "mystery.bin");

        format.Format.ShouldBe(DetectedFormat.Unknown);
        format.ContentType.ShouldBe("application/octet-stream");
    }

    [Fact]
    public void Detect_WhenAZipIsNotAnOfficeFile_SaysUnknown()
    {
        var format = FormatDetector.Detect(Encoding.ASCII.GetBytes("PK\u0003\u0004photos/a.jpg"), "photos.zip");

        format.Format.ShouldBe(DetectedFormat.Unknown);
    }

    [Fact]
    public void Detect_WhenUtf8TextIsCutMidLetterAtTheSampleEnd_StillSeesText()
    {
        var text = new string('a', 8191) + "é";

        var format = FormatDetector.Detect(Encoding.UTF8.GetBytes(text), "long.txt");

        format.Format.ShouldBe(DetectedFormat.Text);
    }

    [Fact]
    public void FileFormat_ForPhotosAndDrawings_SaysWhichKindItIs()
    {
        new FileFormat(DetectedFormat.Heic, "image/heic", true).IsImage.ShouldBeTrue();
        new FileFormat(DetectedFormat.Dwg, "image/vnd.dwg", true).IsCad.ShouldBeTrue();
        new FileFormat(DetectedFormat.Pdf, "application/pdf", true).IsImage.ShouldBeFalse();
    }
}
