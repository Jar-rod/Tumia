namespace Tumiia.Domain.Analysis;

/// <summary>How sure TUMIIA is, in three steps. The UI shows these as words, never as a percentage.</summary>
public enum ConfidenceBand
{
    Low,
    Medium,
    High,
}
