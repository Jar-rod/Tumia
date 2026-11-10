namespace Tumiia.Infrastructure.Analysis;

/// <summary>The <c>Analysis</c> configuration section: the background worker that reads the analysis queue.</summary>
public sealed class AnalysisSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Analysis";

    /// <summary>Whether this process reads the analysis queue. True by default; tests switch it off.</summary>
    public bool WorkerEnabled { get; set; } = true;

    /// <summary>How many tries a document gets before it is marked Failed. Matches the queue's dead-letter setting (3).</summary>
    public int MaxAttempts { get; set; } = 3;
}
