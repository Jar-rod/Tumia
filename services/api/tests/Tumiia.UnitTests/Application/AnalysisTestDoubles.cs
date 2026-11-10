using System.Text;
using NSubstitute;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;
using Tumiia.Infrastructure.Analysis.Reading;
using Tumiia.UnitTests.Analysis;

namespace Tumiia.UnitTests.Application;

/// <summary>The fakes the analysis use-case tests need. Reading text and the taxonomy are real; storage and the hosted AI are fake.</summary>
public sealed class AnalysisTestDoubles
{
    public DocumentTestDoubles Documents { get; } = new();

    public IDocumentAnalysisStore Analyses { get; } = Substitute.For<IDocumentAnalysisStore>();

    public ITextReader Reader { get; } = Substitute.For<ITextReader>();

    public ITaxonomyStore Taxonomy { get; } = Substitute.For<ITaxonomyStore>();

    public IHostedDocumentAnalyser Hosted { get; } = Substitute.For<IHostedDocumentAnalyser>();

    public AnalysisTestDoubles()
    {
        Taxonomy.ListAsync(Arg.Any<CancellationToken>()).Returns(SampleDocuments.Taxonomy);
        Reader.ReadAsync(Arg.Any<byte[]>(), Arg.Any<FileFormat>(), Arg.Any<CancellationToken>())
            .Returns(call => PlainTextReader.Read(call.ArgAt<byte[]>(0)));
        Hosted.AnalyseAsync(Arg.Any<DocumentContent>(), Arg.Any<IReadOnlyList<TaxonomyCategory>>(), Arg.Any<CancellationToken>())
            .Returns(HostedAnalysisResult.NotUsed);
    }

    public AnalyseDocument AnalyseDocument() => new(
        Documents.Documents, Analyses, Documents.Files, Reader, Taxonomy, Hosted, new ConfidenceWeights(), Documents.AuditLog, Documents.Clock);

    /// <summary>An uploaded .txt document whose stored file holds <paramref name="text"/>.</summary>
    public Document UploadedText(string text, string fileName = "quote.txt")
    {
        var document = Document.AwaitUpload(DocumentTestDoubles.Project.Id, new UploadFile(fileName, "text/plain", text.Length, new string('d', 64)), CaptureSource.DragDrop, DocumentTestDoubles.MemberId);
        document.MarkUploaded(Documents.Clock.GetUtcNow());
        Documents.Documents.FindAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        Documents.Files.DownloadAsync(document.StorageKey, Arg.Any<CancellationToken>()).Returns(Encoding.UTF8.GetBytes(text));
        return document;
    }
}
