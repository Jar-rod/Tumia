using NSubstitute;
using Tumiia.Application.Analysis;
using Tumiia.Application.Documents;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;
using Tumiia.UnitTests.Analysis;

namespace Tumiia.UnitTests.Application;

public class GetDocumentTests
{
    private readonly DocumentTestDoubles _fakes = new();
    private readonly IDocumentAnalysisStore _analyses = Substitute.For<IDocumentAnalysisStore>();

    private Task<DocumentDetails?> GetAsync(Guid userId, Guid documentId) => new GetDocument(_fakes.Access(), _analyses).ExecuteAsync(userId, documentId, CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_WhenTheDocumentWasAnalysed_ReturnsItsLatestAnalysis()
    {
        var document = _fakes.StoredDocument(uploaded: true);
        document.StartAnalysis();
        var interpretation = DocumentInterpreter.Interpret(new InterpretationRequest(SampleDocuments.Typed(SampleDocuments.Quote), SampleDocuments.Pdf(), "q.pdf", SampleDocuments.Taxonomy, null, new ConfidenceWeights()));
        var analysis = DocumentAnalysis.Record(document.Id, "1.0", DateTimeOffset.UtcNow, interpretation, "application/pdf", new AnalyserIdentity("local", null));
        document.RecordAnalysis(analysis.Id, "application/pdf", true, ConsequenceLevel.Medium, interpretation.Outcome);
        _analyses.FindAsync(analysis.Id, Arg.Any<CancellationToken>()).Returns(analysis);

        var details = await GetAsync(DocumentTestDoubles.MemberId, document.Id);

        details!.Document.ShouldBe(document);
        details.LatestAnalysis.ShouldBe(analysis);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotAnalysedYet_ReturnsNoAnalysis()
    {
        var document = _fakes.StoredDocument(uploaded: true);

        var details = await GetAsync(DocumentTestDoubles.MemberId, document.Id);

        details!.LatestAnalysis.ShouldBeNull();
        await _analyses.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheCallerIsNotAMember_ReturnsNull()
    {
        var document = _fakes.StoredDocument(uploaded: true);

        (await GetAsync(DocumentTestDoubles.OutsiderId, document.Id)).ShouldBeNull();
    }
}
