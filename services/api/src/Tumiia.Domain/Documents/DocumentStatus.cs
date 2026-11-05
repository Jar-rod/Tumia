namespace Tumiia.Domain.Documents;

/// <summary>Where a document is in its life, from upload to a person confirming it.</summary>
public enum DocumentStatus
{
    /// <summary>The browser was given an upload address but has not said the upload finished. Not in the v1.1 list; needed between the two upload calls.</summary>
    PendingUpload,

    /// <summary>Uploaded and queued for analysis.</summary>
    New,

    Processing,

    AutoFiled,

    AwaitingConfirmation,

    NeedsReview,

    Confirmed,

    Failed,
}
