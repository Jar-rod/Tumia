using NSubstitute;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;
using Tumiia.UnitTests.Analysis;

namespace Tumiia.UnitTests.Application;

public class AnalyseDocumentTests
{
    private readonly AnalysisTestDoubles _fakes = new();

    private void HostedAnswers(HostedAnalysisResult result)
    {
        _fakes.Hosted.AnalyseAsync(Arg.Any<DocumentContent>(), Arg.Any<IReadOnlyList<TaxonomyCategory>>(), Arg.Any<CancellationToken>()).Returns(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheAiAnswers_SavesItsProposalAndMovesTheDocumentOn()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        HostedAnswers(new HostedAnalysisResult(SampleDocuments.Hosted("commercial_quote", fields: [new HostedField("amount", "18450.00", 0.95, 1, "Total R 18,450.00")]), null));
        DocumentAnalysis? saved = null;
        await _fakes.Analyses.AddWithDocumentAsync(Arg.Do<DocumentAnalysis>(analysis => saved = analysis), Arg.Any<CancellationToken>());

        var outcome = await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        outcome.ShouldBe(AnalyseDocumentOutcome.Analysed);
        saved.ShouldNotBeNull();
        saved.ProposedCategory.ShouldBe("commercial_quote");
        saved.PipelineVersion.ShouldBe(AnalysisPipeline.Version);
        saved.AnalysedAt.ShouldBe(_fakes.Documents.Clock.GetUtcNow());
        saved.Analyser.ShouldBe(new AnalyserIdentity("bedrock", "anthropic.claude-haiku-5-5"));
        document.Status.ShouldBe(DocumentStatus.AutoFiled);
        document.LatestAnalysisId.ShouldBe(saved.Id);
        document.ContentTypeDetected.ShouldBe("text/plain");
        document.ConsequenceLevel.ShouldBe(ConsequenceLevel.Medium);
        await _fakes.Documents.AuditLog.Received().RecordAsync(AuditEventType.DocumentAnalysed, null, AnalysisPipeline.SystemIpAddress, true, Arg.Any<CancellationToken>(), document.Id);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheOriginalItsFormatAndTheTextReadToTheAi()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote, "whatsapp.txt");

        await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        await _fakes.Hosted.Received().AnalyseAsync(
            Arg.Is<DocumentContent>(content => content.FileName == "whatsapp.txt" && content.Format.Format == DetectedFormat.Text && content.Text.AllText.Contains("Total R 18,450.00") && content.Bytes.Length > 0),
            SampleDocuments.Taxonomy,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheAiIsOff_SortsByKeywordsAndSendsItToNeedsReview()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        DocumentAnalysis? saved = null;
        await _fakes.Analyses.AddWithDocumentAsync(Arg.Do<DocumentAnalysis>(analysis => saved = analysis), Arg.Any<CancellationToken>());

        await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        saved!.ProposedCategory.ShouldBe("commercial_quote");
        saved.Analyser.ShouldBe(new AnalyserIdentity("local", null));
        document.Status.ShouldBe(DocumentStatus.NeedsReview);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheAiDidNotAnswer_KeepsItsReasonWithTheAnalysis()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        HostedAnswers(new HostedAnalysisResult(null, "The AI service could not be reached."));
        DocumentAnalysis? saved = null;
        await _fakes.Analyses.AddWithDocumentAsync(Arg.Do<DocumentAnalysis>(analysis => saved = analysis), Arg.Any<CancellationToken>());

        await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        saved!.Warnings.ShouldContain("The AI service could not be reached.");
        document.Status.ShouldBe(DocumentStatus.NeedsReview);
    }

    [Fact]
    public async Task ExecuteAsync_MarksTheDocumentProcessingBeforeReadingIt()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        DocumentStatus? statusWhenRead = null;
        _fakes.Documents.Files.DownloadAsync(document.StorageKey, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                statusWhenRead = document.Status;
                return "Total R 10.00"u8.ToArray();
            });

        await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        statusWhenRead.ShouldBe(DocumentStatus.Processing);
        await _fakes.Documents.Documents.Received().SaveAsync(document, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentWasAlreadyAnalysed_DoesNothing()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        _fakes.Analyses.ExistsAsync(document.Id, AnalysisPipeline.Version, Arg.Any<CancellationToken>()).Returns(true);

        var outcome = await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None);

        outcome.ShouldBe(AnalyseDocumentOutcome.AlreadyAnalysed);
        document.Status.ShouldBe(DocumentStatus.New);
        await _fakes.Documents.Files.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _fakes.Hosted.DidNotReceive().AnalyseAsync(Arg.Any<DocumentContent>(), Arg.Any<IReadOnlyList<TaxonomyCategory>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentHasMovedOn_DoesNothing()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        document.MarkAnalysisFailed("earlier");

        (await _fakes.AnalyseDocument().ExecuteAsync(document.Id, CancellationToken.None)).ShouldBe(AnalyseDocumentOutcome.AlreadyAnalysed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentDoesNotExistOrHasNoFileYet_SaysNotFound()
    {
        var waiting = _fakes.Documents.StoredDocument();

        (await _fakes.AnalyseDocument().ExecuteAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBe(AnalyseDocumentOutcome.NotFound);
        (await _fakes.AnalyseDocument().ExecuteAsync(waiting.Id, CancellationToken.None)).ShouldBe(AnalyseDocumentOutcome.NotFound);
    }

    [Fact]
    public async Task NoHostedAnalyser_AlwaysSaysNotUsed()
    {
        var result = await new NoHostedAnalyser().AnalyseAsync(new DocumentContent([], SampleDocuments.Pdf(), "a.pdf", new DocumentText()), [], CancellationToken.None);

        result.ShouldBe(HostedAnalysisResult.NotUsed);
    }
}
