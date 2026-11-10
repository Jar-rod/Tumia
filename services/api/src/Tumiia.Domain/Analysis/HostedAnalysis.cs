namespace Tumiia.Domain.Analysis;

/// <summary>What the hosted AI (Claude on Amazon Bedrock) said about a document, as it said it.</summary>
/// <param name="CategoryKey">The category it proposed.</param>
/// <param name="Confidence">How sure it said it was about the category, from 0 to 1.</param>
/// <param name="RunnerUpKey">The next most likely category, or null.</param>
/// <param name="RunnerUpConfidence">How likely the runner-up is, from 0 to 1.</param>
/// <param name="Readability">How legible the document was to it, from 0 (unreadable) to 1 (clear).</param>
/// <param name="Language">The document's language as an ISO 639-1 code, or "und".</param>
/// <param name="Fields">The facts it found.</param>
/// <param name="Warnings">Problems it noticed, in plain words.</param>
/// <param name="Model">The model that answered, recorded with the analysis.</param>
public sealed record HostedAnalysis(
    string CategoryKey,
    double Confidence,
    string? RunnerUpKey,
    double RunnerUpConfidence,
    double Readability,
    string Language,
    IReadOnlyList<HostedField> Fields,
    IReadOnlyList<string> Warnings,
    string Model);
