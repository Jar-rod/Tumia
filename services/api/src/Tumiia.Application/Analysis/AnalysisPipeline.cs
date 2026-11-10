namespace Tumiia.Application.Analysis;

/// <summary>Facts about the analysis steps as a whole.</summary>
public static class AnalysisPipeline
{
    /// <summary>
    /// The version of the analysis steps, stored with every analysis. Raise it when the steps change in a way that
    /// should give different results; a document is analysed at most once per version.
    /// </summary>
    public const string Version = "2.0";

    /// <summary>The address recorded in the audit log for things the system did by itself.</summary>
    public const string SystemIpAddress = "system";
}
