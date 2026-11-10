using System.Security.Cryptography;
using System.Text;
using NSubstitute;
using Tumiia.Application.Documents;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Application;

public class CapturePastedTextTests
{
    private readonly DocumentTestDoubles _fakes = new();

    private CapturePastedText UseCase()
    {
        var rules = new UploadRules { AllowedExtensions = new HashSet<string> { ".txt" }, MaxFileSizeMb = 1 };
        return new CapturePastedText(_fakes.Access(), _fakes.Documents, _fakes.Files, _fakes.Queue, _fakes.AuditLog, rules, _fakes.Clock);
    }

    private Task<CapturePastedTextResult> PasteAsync(string text, string? title = null, Guid? userId = null)
    {
        return UseCase().ExecuteAsync(userId ?? DocumentTestDoubles.MemberId, DocumentTestDoubles.Project.Id, text, title, "10.0.0.1", CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewTextIsPasted_StoresItUnchangedMarksItNewAndQueuesIt()
    {
        const string text = "Please deliver 2000 bricks on Monday.\r\nThanks, Sipho";

        var result = await PasteAsync(text, "Site instruction");

        var document = result.Document.ShouldNotBeNull();
        result.Duplicate.ShouldBeFalse();
        document.OriginalFilename.ShouldBe("Site instruction.txt");
        document.CaptureSource.ShouldBe(CaptureSource.Paste);
        document.Status.ShouldBe(DocumentStatus.New);
        document.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))));
        await _fakes.Files.Received().UploadAsync(document.StorageKey, Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == text), CapturePastedText.ContentType, Arg.Any<CancellationToken>());
        await _fakes.Queue.Received().EnqueueForAnalysisAsync(document.Id, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.DocumentUploaded, DocumentTestDoubles.MemberId, "10.0.0.1", true, Arg.Any<CancellationToken>(), document.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheSameTextWasPastedBefore_ReturnsThatDocument()
    {
        var existing = _fakes.StoredDocument("earlier.txt", uploaded: true);
        _fakes.Documents.FindByHashAsync(DocumentTestDoubles.Project.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await PasteAsync("Same text");

        result.Duplicate.ShouldBeTrue();
        result.Document.ShouldBe(existing);
        await _fakes.Files.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNothingWasPasted_SaysSo()
    {
        var result = await PasteAsync("   ");

        result.Error.ShouldBe("Paste some text first.");
        result.Document.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheTextIsTooBig_RefusesIt()
    {
        var result = await PasteAsync(new string('a', (1024 * 1024) + 1));

        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("1 MB");
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheCallerIsNotAMember_SaysTheProjectWasNotFound()
    {
        var result = await PasteAsync("Hello", userId: DocumentTestDoubles.OutsiderId);

        result.ProjectFound.ShouldBeFalse();
        await _fakes.Documents.DidNotReceive().AddOrGetExistingAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "Pasted text 2026-11-05 0900.txt")]
    [InlineData("  ", "Pasted text 2026-11-05 0900.txt")]
    [InlineData("Quote: bricks/cement?", "Quote brickscement.txt")]
    public void FileNameFor_UsesTheTitleWithoutCharactersFileNamesCannotHave(string? title, string expected)
    {
        UseCase().FileNameFor(title).ShouldBe(expected);
    }

    [Fact]
    public void FileNameFor_WhenTheTitleIsLong_CutsItTo100Characters()
    {
        UseCase().FileNameFor(new string('x', 150)).ShouldBe(new string('x', 100) + ".txt");
    }
}
