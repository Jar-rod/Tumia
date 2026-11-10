using Tumiia.Domain.Documents;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// Decides what happens to a document after its analysis (v1.1 section 4):
/// sure and not high-consequence: filed automatically, and reversible with Undo;
/// fairly sure, or any high-consequence category (payment, contract, legal) however sure: waits for a person to confirm;
/// not sure, or nothing recognised: Needs Review, with the best guess kept as a suggestion.
/// Quotes and invoices (medium consequence) can be filed automatically when TUMIIA is sure, as in the brief's
/// "Quote · Bricks · R18,450 — Filed ✓" example.
/// </summary>
public static class FilingDecider
{
    /// <summary>The status the document moves to.</summary>
    public static DocumentStatus Decide(ConfidenceBand band, TaxonomyCategory proposed)
    {
        if (proposed.IsNeedsReview || band == ConfidenceBand.Low)
        {
            return DocumentStatus.NeedsReview;
        }

        if (proposed.Consequence == ConsequenceLevel.High || band == ConfidenceBand.Medium)
        {
            return DocumentStatus.AwaitingConfirmation;
        }

        return DocumentStatus.AutoFiled;
    }
}
