using NSubstitute;
using Tumiia.Application.Documents;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Application;

public class RequestUploadsTests
{
    private readonly DocumentTestDoubles _fakes = new();

    private static UploadFile File(string name, char hashCharacter = 'a') => new(name, "application/pdf", 1000, new string(hashCharacter, 64));

    private Task<RequestUploadsResult> RequestAsync(Guid userId, params UploadFile[] files)
    {
        var requestUploads = new RequestUploads(_fakes.Access(), _fakes.Documents, _fakes.Files, _fakes.AuditLog, _fakes.Rules);
        return requestUploads.ExecuteAsync(userId, DocumentTestDoubles.Project.Id, files, CaptureSource.DragDrop, "10.0.0.1", CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotAMember_SaysTheProjectWasNotFound()
    {
        var result = await RequestAsync(DocumentTestDoubles.OutsiderId, File("a.pdf"));

        result.ProjectFound.ShouldBeFalse();
        await _fakes.Documents.DidNotReceive().AddOrGetExistingAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileIsNew_CreatesADocumentAndAnUploadAddressSignedForItsSize()
    {
        var result = await RequestAsync(DocumentTestDoubles.MemberId, File("a.pdf"));

        var target = result.Targets.ShouldHaveSingleItem();
        target.DocumentId.ShouldNotBeNull();
        target.UploadUrl.ShouldBe($"https://s3.test/originals/{DocumentTestDoubles.Project.Id}/{target.DocumentId}");
        await _fakes.Files.Received().CreateUploadUrlAsync(Arg.Any<string>(), "application/pdf", 1000, TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.DocumentUploadRequested, DocumentTestDoubles.MemberId, "10.0.0.1", true, Arg.Any<CancellationToken>(), target.DocumentId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneFileIsNotAllowed_RefusesTheWholeRequest()
    {
        var result = await RequestAsync(DocumentTestDoubles.MemberId, File("a.pdf"), File("b.exe", 'b'));

        result.Errors.ShouldContainKey("files[1]");
        result.Targets.ShouldBeEmpty();
        await _fakes.Documents.DidNotReceive().AddOrGetExistingAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheSameFileIsAlreadyUploaded_ReturnsTheExistingDocument()
    {
        var existing = _fakes.StoredDocument(uploaded: true);
        _fakes.Documents.FindByHashAsync(DocumentTestDoubles.Project.Id, existing.Sha256, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await RequestAsync(DocumentTestDoubles.MemberId, File("copy.pdf", 'c'));

        var target = result.Targets.ShouldHaveSingleItem();
        target.ExistingDocumentId.ShouldBe(existing.Id);
        target.UploadUrl.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnEarlierUploadOfTheFileNeverFinished_GivesThatDocumentAFreshAddress()
    {
        var unfinished = _fakes.StoredDocument(uploaded: false);
        _fakes.Documents.FindByHashAsync(DocumentTestDoubles.Project.Id, unfinished.Sha256, Arg.Any<CancellationToken>()).Returns(unfinished);

        var result = await RequestAsync(DocumentTestDoubles.MemberId, File("quote.pdf", 'c'));

        var target = result.Targets.ShouldHaveSingleItem();
        target.DocumentId.ShouldBe(unfinished.Id);
        target.UploadUrl.ShouldNotBeNull();
        await _fakes.Documents.DidNotReceive().AddOrGetExistingAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }
}
