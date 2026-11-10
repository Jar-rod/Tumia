namespace Tumiia.Domain.Analysis;

/// <summary>
/// How much each signal counts towards the classification confidence. They should add up to 1. The defaults come
/// from the build plan; they can be changed in configuration (<c>Analysis:Weights</c>) as error rates are measured.
/// </summary>
public sealed record ConfidenceWeights
{
    public double WinnerProbability { get; init; } = 0.30;

    public double Margin { get; init; } = 0.15;

    public double Readability { get; init; } = 0.15;

    public double Agreement { get; init; } = 0.20;

    public double ExpectedFieldsFound { get; init; } = 0.10;

    public double FormatMatches { get; init; } = 0.10;
}
