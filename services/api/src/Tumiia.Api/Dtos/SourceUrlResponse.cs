namespace Tumiia.Api.Dtos;

/// <summary>A short-lived address for the original file.</summary>
/// <param name="Url">Open or download the file here. PDFs and images open in the browser; other files download.</param>
/// <param name="ExpiresAt">When the address stops working (UTC). Ask again after that.</param>
public sealed record SourceUrlResponse(string Url, DateTimeOffset ExpiresAt);
