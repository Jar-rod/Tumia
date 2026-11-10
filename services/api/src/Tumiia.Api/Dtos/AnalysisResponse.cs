using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.Api.Dtos;

/// <summary>What the AI proposed for a document. It is never changed; a person's confirmation is stored separately.</summary>
/// <param name="Id">The analysis id.</param>
/// <param name="AnalysedAt">When it ran (UTC).</param>
/// <param name="PipelineVersion">The version of the analysis steps.</param>
/// <param name="DetectedLanguage">ISO 639-1 language code, or "und" when it could not be told.</param>
/// <param name="ProposedCategory">The proposed category key, such as commercial_quote.</param>
/// <param name="ProposedCategoryName">The proposed category's display name, such as "Quote".</param>
/// <param name="RunnerUpCategory">The second most likely category key, if any.</param>
/// <param name="ClassificationConfidence">How sure the AI is about the category, from 0 to 1. Show it as words.</param>
/// <param name="Band">The confidence as High, Medium or Low, using the category's thresholds.</param>
/// <param name="Outcome">What the analysis decided: AutoFiled, AwaitingConfirmation or NeedsReview.</param>
/// <param name="Summary">One plain line, such as "Quote from ABC Supplies for Bricks — R18,450.00, 12 Oct 2026."</param>
/// <param name="Fields">The facts found, each with its own confidence and evidence. The first is always document_type.</param>
/// <param name="Signals">The signals behind the classification confidence.</param>
/// <param name="Warnings">Reading problems and confidence limits, in plain words.</param>
public sealed record AnalysisResponse(
    Guid Id,
    DateTimeOffset AnalysedAt,
    string PipelineVersion,
    string DetectedLanguage,
    string ProposedCategory,
    string ProposedCategoryName,
    string? RunnerUpCategory,
    double ClassificationConfidence,
    ConfidenceBand Band,
    DocumentStatus Outcome,
    string Summary,
    IReadOnlyList<ExtractedFieldResponse> Fields,
    ConfidenceSignals Signals,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Copies an analysis.</summary>
    public static AnalysisResponse From(DocumentAnalysis analysis) => new(
        analysis.Id,
        analysis.AnalysedAt,
        analysis.PipelineVersion,
        analysis.DetectedLanguage,
        analysis.ProposedCategory,
        analysis.ProposedCategoryName,
        analysis.RunnerUpCategory,
        analysis.ClassificationConfidence,
        analysis.Band,
        analysis.Outcome,
        analysis.Summary,
        analysis.Fields.Select(ExtractedFieldResponse.From).ToList(),
        analysis.Signals,
        analysis.Warnings);
}
