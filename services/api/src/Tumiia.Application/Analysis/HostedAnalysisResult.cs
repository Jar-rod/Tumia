using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>The hosted AI's answer, or why there is none.</summary>
/// <param name="Analysis">What the AI said, or null when it was not used or did not answer.</param>
/// <param name="Warning">Why there is no answer, in plain words, or null when it answered (or is switched off).</param>
public sealed record HostedAnalysisResult(HostedAnalysis? Analysis, string? Warning)
{
    /// <summary>The result when the hosted AI is switched off.</summary>
    public static readonly HostedAnalysisResult NotUsed = new(null, null);
}
