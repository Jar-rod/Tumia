using Tumiia.Domain.Documents;

namespace Tumiia.Api.Dtos;

/// <summary>A document, as shown in lists.</summary>
/// <param name="Id">The document id.</param>
/// <param name="ProjectId">The project it belongs to.</param>
/// <param name="OriginalFilename">The file's name when uploaded.</param>
/// <param name="ContentTypeClaimed">The file type the browser reported.</param>
/// <param name="SizeBytes">The file's size.</param>
/// <param name="CaptureSource">How the file arrived: drag_drop, file_picker, mobile_upload or paste.</param>
/// <param name="Status">Where the document is: New, Processing, AutoFiled, AwaitingConfirmation, NeedsReview, Confirmed or Failed.</param>
/// <param name="UploadTimestamp">When the upload finished (UTC).</param>
/// <param name="ConsequenceLevel">How much a wrong filing would matter, once analysed.</param>
/// <param name="NeedsReview">True when a person must look at it.</param>
/// <param name="DocumentTypeConfirmed">The category a person confirmed, if any.</param>
/// <param name="SubjectConfirmed">The subject a person confirmed, if any.</param>
public sealed record DocumentResponse(
    Guid Id,
    Guid ProjectId,
    string OriginalFilename,
    string ContentTypeClaimed,
    long SizeBytes,
    CaptureSource CaptureSource,
    DocumentStatus Status,
    DateTimeOffset? UploadTimestamp,
    ConsequenceLevel? ConsequenceLevel,
    bool NeedsReview,
    string? DocumentTypeConfirmed,
    string? SubjectConfirmed)
{
    /// <summary>Copies the fields shown in lists.</summary>
    public static DocumentResponse From(Document document) => new(
        document.Id,
        document.ProjectId,
        document.OriginalFilename,
        document.ContentTypeClaimed,
        document.SizeBytes,
        document.CaptureSource,
        document.Status,
        document.UploadTimestamp,
        document.ConsequenceLevel,
        document.NeedsReview,
        document.DocumentTypeConfirmed,
        document.SubjectConfirmed);
}
