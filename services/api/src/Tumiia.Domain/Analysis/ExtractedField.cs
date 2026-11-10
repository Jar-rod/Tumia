namespace Tumiia.Domain.Analysis;

/// <summary>One fact found in a document, how sure TUMIIA is about it, and where it came from.</summary>
/// <param name="Name">The field name, such as <c>amount</c> or <c>party</c>.</param>
/// <param name="Value">The value, normalised: dates as yyyy-MM-dd, amounts as plain numbers, currencies as ISO codes.</param>
/// <param name="Confidence">From 0 to 1, for this field alone.</param>
/// <param name="Evidence">Where it was found.</param>
/// <param name="Method">How it was found: <c>hosted</c> (the AI on Bedrock), <c>regex</c>, <c>keyword</c> or <c>exif</c>.</param>
public sealed record ExtractedField(string Name, string Value, double Confidence, FieldEvidence Evidence, string Method);
