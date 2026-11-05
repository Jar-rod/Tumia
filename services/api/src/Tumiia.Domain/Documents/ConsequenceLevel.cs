namespace Tumiia.Domain.Documents;

/// <summary>How much harm a wrong filing would do. High (payment, contract, legal) always needs a person to confirm.</summary>
public enum ConsequenceLevel
{
    Low,
    Medium,
    High,
}
