using Tumiia.Domain.Documents;

namespace Tumiia.Domain.Analysis;

/// <summary>What TUMIIA made of a document: its category, how sure it is, the facts found, and where the document goes.</summary>
/// <param name="Category">The proposed category (Needs Review when nothing was recognised).</param>
/// <param name="RunnerUpCategory">The second most likely category key, if any.</param>
/// <param name="Language">The detected language code.</param>
/// <param name="Confidence">How sure TUMIIA is about the category.</param>
/// <param name="Outcome">AutoFiled, AwaitingConfirmation or NeedsReview.</param>
/// <param name="Fields">The facts found, starting with the document type.</param>
/// <param name="Summary">The one-line summary.</param>
/// <param name="Warnings">Reading problems and confidence limits, in plain words.</param>
public sealed record Interpretation(
    TaxonomyCategory Category,
    string? RunnerUpCategory,
    string Language,
    ClassificationConfidence Confidence,
    DocumentStatus Outcome,
    IReadOnlyList<ExtractedField> Fields,
    string Summary,
    IReadOnlyList<string> Warnings);
