namespace Tumiia.Domain.Analysis;

/// <summary>How sure TUMIIA is about a document's category, why, and which limits were applied.</summary>
/// <param name="Score">From 0 to 1.</param>
/// <param name="Band">The score as High, Medium or Low, using the category's thresholds.</param>
/// <param name="Signals">The signals the score was built from.</param>
/// <param name="CapsApplied">The limits that lowered the score, in plain words (for example "the rules and the model disagree").</param>
public sealed record ClassificationConfidence(double Score, ConfidenceBand Band, ConfidenceSignals Signals, IReadOnlyList<string> CapsApplied);
