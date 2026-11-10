using NSubstitute;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Application;

public class FailDocumentAnalysisTests
{
    private readonly DocumentTestDoubles _fakes = new();

    private Task FailAsync(Guid documentId) => new FailDocumentAnalysis(_fakes.Documents, _fakes.AuditLog).ExecuteAsync(documentId, "Gave up.", CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentIsStillBeingAnalysed_MarksItFailedAndRecordsIt()
    {
        var document = _fakes.StoredDocument(uploaded: true);
        document.StartAnalysis();

        await FailAsync(document.Id);

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason.ShouldBe("Gave up.");
        await _fakes.Documents.Received().SaveAsync(document, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.DocumentAnalysisFailed, null, AnalysisPipeline.SystemIpAddress, false, Arg.Any<CancellationToken>(), document.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentIsMissingOrAlreadyAnalysed_ChangesNothing()
    {
        var document = _fakes.StoredDocument(uploaded: true);
        document.StartAnalysis();
        document.RecordAnalysis(Guid.NewGuid(), "application/pdf", true, null, DocumentStatus.AutoFiled);

        await FailAsync(document.Id);
        await FailAsync(Guid.NewGuid());

        document.Status.ShouldBe(DocumentStatus.AutoFiled);
        await _fakes.Documents.DidNotReceive().SaveAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }
}
