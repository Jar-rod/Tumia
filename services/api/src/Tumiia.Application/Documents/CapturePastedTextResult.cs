using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>The result of pasting text into a project.</summary>
/// <param name="ProjectFound">False when the project does not exist or the caller is not a member.</param>
/// <param name="Error">Why the text was refused, in plain words, or null.</param>
/// <param name="Document">The new document, or the existing one when the same text was pasted before.</param>
/// <param name="Duplicate">True when exactly this text was already in the project.</param>
public sealed record CapturePastedTextResult(bool ProjectFound, string? Error, Document? Document, bool Duplicate);
