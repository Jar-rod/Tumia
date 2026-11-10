using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Storage;

namespace Tumiia.Application.Analysis;

/// <summary>
/// Analyses one uploaded document: downloads the original (which is never changed), detects its real format, reads
/// what it can itself (typed text, photo EXIF), asks the hosted AI (Claude on Amazon Bedrock) what the document is
/// and what is in it, checks the answer, and decides whether it is filed automatically, waits for confirmation, or
/// goes to Needs Review. If the AI is off or does not answer, the keyword rules decide. Safe to run twice for the same
/// document: the second run does nothing.
/// </summary>
public sealed class AnalyseDocument(
    IDocumentStore documents,
    IDocumentAnalysisStore analyses,
    IFileStore files,
    ITextReader reader,
    ITaxonomyStore taxonomy,
    IHostedDocumentAnalyser hosted,
    ConfidenceWeights weights,
    IAuditLog auditLog,
    TimeProvider clock)
{
    /// <summary>Analyses the document; see <see cref="AnalyseDocumentOutcome"/> for the possible results.</summary>
    public async Task<AnalyseDocumentOutcome> ExecuteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documents.FindAsync(documentId, cancellationToken);
        if (document is null || !document.IsUploaded)
        {
            return AnalyseDocumentOutcome.NotFound;
        }

        if (!document.IsAwaitingAnalysis || await analyses.ExistsAsync(documentId, AnalysisPipeline.Version, cancellationToken))
        {
            return AnalyseDocumentOutcome.AlreadyAnalysed;
        }

        document.StartAnalysis();
        await documents.SaveAsync(document, cancellationToken);

        var content = await files.DownloadAsync(document.StorageKey, cancellationToken);
        var format = FormatDetector.Detect(content, document.OriginalFilename);
        var text = await reader.ReadAsync(content, format, cancellationToken);
        var categories = await taxonomy.ListAsync(cancellationToken);
        var answer = await hosted.AnalyseAsync(new DocumentContent(content, format, document.OriginalFilename, text), categories, cancellationToken);
        var interpretation = Interpret(text, format, document.OriginalFilename, categories, answer);

        var analyser = answer.Analysis is { } used ? new AnalyserIdentity("bedrock", used.Model) : new AnalyserIdentity("local", null);
        var analysis = DocumentAnalysis.Record(document.Id, AnalysisPipeline.Version, clock.GetUtcNow(), interpretation, format.ContentType, analyser);
        document.RecordAnalysis(analysis.Id, format.ContentType, format.ExtensionMatches, interpretation.Category.Consequence, interpretation.Outcome);
        await analyses.AddWithDocumentAsync(analysis, cancellationToken);
        await auditLog.RecordAsync(AuditEventType.DocumentAnalysed, null, AnalysisPipeline.SystemIpAddress, true, cancellationToken, document.Id);
        return AnalyseDocumentOutcome.Analysed;
    }

    // The AI's reason for not answering is kept with the analysis, so a person can see why it was sorted by keywords.
    private Interpretation Interpret(DocumentText text, FileFormat format, string fileName, IReadOnlyList<TaxonomyCategory> categories, HostedAnalysisResult answer)
    {
        var withWarning = answer.Warning is null ? text : text with { Warnings = [.. text.Warnings, answer.Warning] };
        return DocumentInterpreter.Interpret(new InterpretationRequest(withWarning, format, fileName, categories, answer.Analysis, weights));
    }
}
