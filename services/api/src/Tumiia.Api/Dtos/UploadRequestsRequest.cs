using System.ComponentModel.DataAnnotations;
using Tumiia.Domain.Documents;

namespace Tumiia.Api.Dtos;

/// <summary>The files the browser wants to upload in one go, and how they were picked.</summary>
public sealed record UploadRequestsRequest
{
    /// <summary>How the files were picked: <c>drag_drop</c>, <c>file_picker</c> or <c>mobile_upload</c>.</summary>
    [Required]
    public required CaptureSource CaptureSource { get; init; }

    /// <summary>The files. At most 50 per request (configurable).</summary>
    [Required]
    public required IReadOnlyList<UploadFileRequest> Files { get; init; }
}
