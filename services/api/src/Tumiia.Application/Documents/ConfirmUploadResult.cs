using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>What happened when the browser said an upload finished.</summary>
public enum ConfirmUploadOutcome
{
    /// <summary>The file is in storage and the document is queued for analysis (or already was).</summary>
    Queued,

    /// <summary>The document does not exist, or the caller cannot access it.</summary>
    NotFound,

    /// <summary>No file has arrived in storage yet.</summary>
    FileMissing,

    /// <summary>The stored file is not the size that was announced.</summary>
    SizeMismatch,
}

/// <summary>The outcome, and the document when there is one.</summary>
public sealed record ConfirmUploadResult(ConfirmUploadOutcome Outcome, Document? Document);
