namespace Tumiia.Domain.Analysis;

/// <summary>Where a fact was found, so every extracted value can be traced back to the original file.</summary>
/// <param name="Source">What it was read from: <c>bedrock</c> (the AI read it), <c>text</c>, <c>exif</c> or <c>classification</c>.</param>
/// <param name="Page">The page (or sheet) number, when it came from the text.</param>
/// <param name="Line">The line number on that page.</param>
/// <param name="Snippet">A short quote of the line it came from, or a note such as "read as day-first".</param>
public sealed record FieldEvidence(string Source, int? Page = null, int? Line = null, string? Snippet = null);
