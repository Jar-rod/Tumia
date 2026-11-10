using Tumiia.Domain.Documents;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// One analysis of one document: what TUMIIA proposed, how sure it was and why, and the fields it found. Analyses
/// are append-only: never changed or removed, even when a person corrects the document, so the AI's original
/// prediction is always kept.
/// </summary>
public sealed class DocumentAnalysis
{
    private DocumentAnalysis()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    /// <summary>The version of the analysis steps. A document is analysed at most once per version.</summary>
    public string PipelineVersion { get; private set; } = string.Empty;

    public DateTimeOffset AnalysedAt { get; private set; }

    /// <summary>The ISO 639-1 language code, or "und" when it could not be told.</summary>
    public string DetectedLanguage { get; private set; } = LanguageDetector.Undetermined;

    public string ContentTypeDetected { get; private set; } = string.Empty;

    /// <summary>The proposed category key, such as <c>commercial_quote</c>.</summary>
    public string ProposedCategory { get; private set; } = string.Empty;

    /// <summary>The proposed category's display name at the time, such as "Quote".</summary>
    public string ProposedCategoryName { get; private set; } = string.Empty;

    public string? RunnerUpCategory { get; private set; }

    public double ClassificationConfidence { get; private set; }

    public ConfidenceBand Band { get; private set; }

    /// <summary>The status the analysis decided on: AutoFiled, AwaitingConfirmation or NeedsReview.</summary>
    public DocumentStatus Outcome { get; private set; }

    public ConfidenceSignals Signals { get; private set; } = new(0, 0, 0, 0, 0, 0);

    public IReadOnlyList<ExtractedField> Fields { get; private set; } = [];

    /// <summary>The one-line summary, built from a template.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Problems met while reading, and the confidence limits applied, in plain words.</summary>
    public IReadOnlyList<string> Warnings { get; private set; } = [];

    public AnalyserIdentity Analyser { get; private set; } = new("local", null);

    /// <summary>Records a finished analysis of <paramref name="documentId"/>.</summary>
    public static DocumentAnalysis Record(Guid documentId, string pipelineVersion, DateTimeOffset analysedAt, Interpretation interpretation, string contentTypeDetected, AnalyserIdentity analyser)
    {
        return new DocumentAnalysis
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PipelineVersion = pipelineVersion,
            AnalysedAt = analysedAt,
            DetectedLanguage = interpretation.Language,
            ContentTypeDetected = contentTypeDetected,
            ProposedCategory = interpretation.Category.Key,
            ProposedCategoryName = interpretation.Category.DisplayName,
            RunnerUpCategory = interpretation.RunnerUpCategory,
            ClassificationConfidence = interpretation.Confidence.Score,
            Band = interpretation.Confidence.Band,
            Outcome = interpretation.Outcome,
            Signals = interpretation.Confidence.Signals,
            Fields = interpretation.Fields,
            Summary = interpretation.Summary,
            Warnings = interpretation.Warnings,
            Analyser = analyser,
        };
    }
}
