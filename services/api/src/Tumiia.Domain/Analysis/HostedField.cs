namespace Tumiia.Domain.Analysis;

/// <summary>One fact as the hosted AI reported it, before TUMIIA checks and normalises it.</summary>
/// <param name="Name">The field name, such as <c>amount</c>.</param>
/// <param name="Value">The value as the AI wrote it.</param>
/// <param name="Confidence">How sure the AI said it was, from 0 to 1.</param>
/// <param name="Page">The page it was found on, or 0 when not known.</param>
/// <param name="Snippet">The words in the document it came from, quoted, or empty.</param>
public sealed record HostedField(string Name, string Value, double Confidence, int Page, string Snippet);
