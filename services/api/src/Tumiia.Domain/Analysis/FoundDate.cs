namespace Tumiia.Domain.Analysis;

/// <summary>A date found in text.</summary>
/// <param name="Date">The date.</param>
/// <param name="Ambiguous">True for dates like 03/04/2026 that could be read either way; TUMIIA reads them day-first.</param>
public sealed record FoundDate(DateOnly Date, bool Ambiguous);
