namespace Tumiia.Application.Documents;

/// <summary>The outcome of an upload request.</summary>
/// <param name="ProjectFound">False when the project does not exist or the caller is not a member.</param>
/// <param name="Errors">Why files were refused, by file position (<c>files[2]</c>) or for the whole request (<c>files</c>).</param>
/// <param name="Targets">One answer per file, in the order sent, when nothing was refused.</param>
public sealed record RequestUploadsResult(bool ProjectFound, IReadOnlyDictionary<string, string[]> Errors, IReadOnlyList<UploadTarget> Targets);
