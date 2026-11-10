namespace Tumiia.Domain.Analysis;

/// <summary>The signals behind a classification confidence, each from 0 to 1. All of them are stored with the analysis.</summary>
/// <param name="WinnerProbability">How likely the proposed category is (the AI's confidence, or the rules' probability).</param>
/// <param name="Margin">How far ahead of the runner-up the proposed category is.</param>
/// <param name="Readability">How legible the document was (the AI's view, or how much typed text there was).</param>
/// <param name="Agreement">1 when the keyword rules agree with the AI, 0 when they disagree, 0.5 when they had no opinion or the AI was not used.</param>
/// <param name="ExpectedFieldsFound">The share of the category's expected fields that were found.</param>
/// <param name="FormatMatches">1 when the real format matches the extension, else 0.</param>
public sealed record ConfidenceSignals(
    double WinnerProbability,
    double Margin,
    double Readability,
    double Agreement,
    double ExpectedFieldsFound,
    double FormatMatches);
