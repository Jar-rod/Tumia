namespace Tumiia.Application.Documents;

/// <summary>The outcome of asking for a document's file address.</summary>
/// <param name="Found">False when the document does not exist or the caller cannot access it.</param>
/// <param name="Url">The address, or null when the file has not been uploaded yet.</param>
/// <param name="ExpiresAt">When the address stops working.</param>
public sealed record SourceUrlResult(bool Found, string? Url, DateTimeOffset? ExpiresAt);
