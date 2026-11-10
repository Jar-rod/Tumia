namespace Tumiia.Api.Dtos;

/// <summary>Where a fact was found in the original file.</summary>
/// <param name="Source">What it was read from: bedrock (the AI read it), text, exif or classification.</param>
/// <param name="Page">The page (or sheet) number, when it came from the text.</param>
/// <param name="Line">The line number on that page.</param>
/// <param name="Snippet">A short quote of the words it came from. Ends "(not found in the document's text)" when the AI's quote could not be checked against the typed text.</param>
public sealed record FieldEvidenceResponse(string Source, int? Page, int? Line, string? Snippet);
