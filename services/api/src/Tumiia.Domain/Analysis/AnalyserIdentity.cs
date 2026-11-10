namespace Tumiia.Domain.Analysis;

/// <summary>Which software produced an analysis, so results can be compared when any part changes.</summary>
/// <param name="Provider"><c>bedrock</c> when the hosted AI read the document, <c>local</c> when only the keyword rules did.</param>
/// <param name="Model">The hosted model, such as <c>anthropic.claude-haiku-5-5</c>, when it was used.</param>
public sealed record AnalyserIdentity(string Provider, string? Model);
