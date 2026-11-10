using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Analysis;

public class DocumentAnalysisStateTests
{
    private static Document Uploaded()
    {
        var document = Document.AwaitUpload(Guid.NewGuid(), new UploadFile("quote.pdf", "application/pdf", 10, new string('a', 64)), CaptureSource.DragDrop, Guid.NewGuid());
        document.MarkUploaded(DateTimeOffset.UtcNow);
        return document;
    }

    [Fact]
    public void StartAnalysis_WhenNewOrAlreadyProcessing_MarksItProcessing()
    {
        var document = Uploaded();

        document.StartAnalysis();
        document.StartAnalysis();

        document.Status.ShouldBe(DocumentStatus.Processing);
    }

    [Fact]
    public void StartAnalysis_WhenStillWaitingForItsFile_Refuses()
    {
        var document = Document.AwaitUpload(Guid.NewGuid(), new UploadFile("a.pdf", "application/pdf", 10, new string('a', 64)), CaptureSource.DragDrop, Guid.NewGuid());

        Should.Throw<InvalidOperationException>(document.StartAnalysis);
    }

    [Fact]
    public void RecordAnalysis_StoresTheOutcomeAndPointsToTheAnalysis()
    {
        var document = Uploaded();
        document.StartAnalysis();
        var analysisId = Guid.NewGuid();

        document.RecordAnalysis(analysisId, "application/pdf", true, ConsequenceLevel.Medium, DocumentStatus.NeedsReview);

        document.LatestAnalysisId.ShouldBe(analysisId);
        document.ContentTypeDetected.ShouldBe("application/pdf");
        document.ExtensionMatches.ShouldBe(true);
        document.ConsequenceLevel.ShouldBe(ConsequenceLevel.Medium);
        document.Status.ShouldBe(DocumentStatus.NeedsReview);
        document.NeedsReview.ShouldBeTrue();
    }

    [Fact]
    public void RecordAnalysis_WhenNotBeingAnalysed_Refuses()
    {
        Should.Throw<InvalidOperationException>(() => Uploaded().RecordAnalysis(Guid.NewGuid(), "x", true, null, DocumentStatus.AutoFiled));
    }

    [Fact]
    public void RecordAnalysis_WhenTheOutcomeIsNotAFilingDecision_Refuses()
    {
        var document = Uploaded();
        document.StartAnalysis();

        Should.Throw<ArgumentOutOfRangeException>(() => document.RecordAnalysis(Guid.NewGuid(), "x", true, null, DocumentStatus.Confirmed));
    }

    [Fact]
    public void MarkAnalysisFailed_MarksItFailedForAPersonAndKeepsAShortReason()
    {
        var document = Uploaded();

        document.MarkAnalysisFailed(new string('x', 600));

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.NeedsReview.ShouldBeTrue();
        document.FailureReason!.Length.ShouldBe(500);
    }

    [Fact]
    public void MarkAnalysisFailed_WhenAlreadyAnalysed_Refuses()
    {
        var document = Uploaded();
        document.StartAnalysis();
        document.RecordAnalysis(Guid.NewGuid(), "x", true, null, DocumentStatus.AutoFiled);

        Should.Throw<InvalidOperationException>(() => document.MarkAnalysisFailed("late"));
    }
}
