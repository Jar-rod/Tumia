using NSubstitute;
using Tumiia.Application.Documents;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Application;

public class ConfirmUploadTests
{
    private readonly DocumentTestDoubles _fakes = new();

    private Task<ConfirmUploadResult> ConfirmAsync(Guid userId, Guid documentId)
    {
        var confirmUpload = new ConfirmUpload(_fakes.Access(), _fakes.Documents, _fakes.Files, _fakes.Queue, _fakes.AuditLog, _fakes.Clock);
        return confirmUpload.ExecuteAsync(userId, documentId, "10.0.0.1", CancellationToken.None);
    }

    private void StoredFileSize(Document document, long? size)
    {
        _fakes.Files.GetSizeAsync(document.StorageKey, Arg.Any<CancellationToken>()).Returns(size);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheFileArrivedWithTheRightSize_MarksItNewQueuesItAndRecordsIt()
    {
        var document = _fakes.StoredDocument();
        StoredFileSize(document, 1000);

        var result = await ConfirmAsync(DocumentTestDoubles.MemberId, document.Id);

        result.Outcome.ShouldBe(ConfirmUploadOutcome.Queued);
        document.Status.ShouldBe(DocumentStatus.New);
        await _fakes.Documents.Received().SaveAsync(document, Arg.Any<CancellationToken>());
        await _fakes.Queue.Received().EnqueueForAnalysisAsync(document.Id, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.DocumentUploaded, DocumentTestDoubles.MemberId, "10.0.0.1", true, Arg.Any<CancellationToken>(), document.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoFileHasArrived_SaysSoAndLeavesTheDocumentWaiting()
    {
        var document = _fakes.StoredDocument();
        StoredFileSize(document, null);

        var result = await ConfirmAsync(DocumentTestDoubles.MemberId, document.Id);

        result.Outcome.ShouldBe(ConfirmUploadOutcome.FileMissing);
        document.Status.ShouldBe(DocumentStatus.PendingUpload);
        await _fakes.Queue.DidNotReceive().EnqueueForAnalysisAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheStoredFileIsAnotherSize_RefusesIt()
    {
        var document = _fakes.StoredDocument();
        StoredFileSize(document, 999);

        var result = await ConfirmAsync(DocumentTestDoubles.MemberId, document.Id);

        result.Outcome.ShouldBe(ConfirmUploadOutcome.SizeMismatch);
        document.Status.ShouldBe(DocumentStatus.PendingUpload);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalledAgainForANewDocument_QueuesItAgainWithoutRecordingTwice()
    {
        var document = _fakes.StoredDocument(uploaded: true);

        var result = await ConfirmAsync(DocumentTestDoubles.MemberId, document.Id);

        result.Outcome.ShouldBe(ConfirmUploadOutcome.Queued);
        await _fakes.Queue.Received(1).EnqueueForAnalysisAsync(document.Id, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.DidNotReceive().RecordAsync(AuditEventType.DocumentUploaded, Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotAMember_SaysNotFound()
    {
        var document = _fakes.StoredDocument();

        var result = await ConfirmAsync(DocumentTestDoubles.OutsiderId, document.Id);

        result.Outcome.ShouldBe(ConfirmUploadOutcome.NotFound);
        result.Document.ShouldBeNull();
    }
}
