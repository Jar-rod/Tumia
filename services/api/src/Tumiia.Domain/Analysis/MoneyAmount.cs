namespace Tumiia.Domain.Analysis;

/// <summary>An amount of money found in a line of text.</summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The ISO 4217 code, or null when the line showed no currency.</param>
public sealed record MoneyAmount(decimal Amount, string? Currency);
