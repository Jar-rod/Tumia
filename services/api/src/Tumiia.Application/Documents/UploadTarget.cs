namespace Tumiia.Application.Documents;

/// <summary>
/// The answer for one file in an upload request: either where to upload it, or the document that already holds
/// the same file.
/// </summary>
/// <param name="FileName">The file name, as sent, so the browser can match answers to files.</param>
/// <param name="DocumentId">The new document's id, when the file should be uploaded.</param>
/// <param name="UploadUrl">The pre-signed address to PUT the file to, when it should be uploaded.</param>
/// <param name="ExistingDocumentId">The document that already has this exact file, when it is a duplicate.</param>
public sealed record UploadTarget(string FileName, Guid? DocumentId, string? UploadUrl, Guid? ExistingDocumentId);
