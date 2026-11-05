namespace Tumiia.Api.Dtos;

/// <summary>
/// What to do with one file: PUT it to <paramref name="UploadUrl"/> (then call <c>/documents/{documentId}/uploaded</c>),
/// or nothing, because <paramref name="ExistingDocumentId"/> already holds the same file.
/// </summary>
/// <param name="FileName">The file name as sent, to match answers to files.</param>
/// <param name="DocumentId">The new document's id, when the file should be uploaded.</param>
/// <param name="UploadUrl">Where to PUT the file, with the same Content-Type and exact size. Works for 5 minutes.</param>
/// <param name="ExistingDocumentId">The document that already has this file, when it is a duplicate.</param>
public sealed record UploadTargetResponse(string FileName, Guid? DocumentId, string? UploadUrl, Guid? ExistingDocumentId);
