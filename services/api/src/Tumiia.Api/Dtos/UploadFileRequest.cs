using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>One file the browser wants to upload.</summary>
public sealed record UploadFileRequest
{
    /// <summary>The file's name, with its extension.</summary>
    [Required]
    [MaxLength(255)]
    public required string FileName { get; init; }

    /// <summary>The file's type as the browser reports it (for example <c>application/pdf</c>). Must be sent again, exactly, on the upload.</summary>
    [Required]
    [MaxLength(255)]
    public required string ContentType { get; init; }

    /// <summary>The file's size in bytes. The upload must be exactly this size.</summary>
    [Range(1, long.MaxValue)]
    public required long SizeBytes { get; init; }

    /// <summary>The SHA-256 of the file's bytes, as 64 hexadecimal characters (Web Crypto <c>crypto.subtle.digest</c>).</summary>
    [Required]
    [RegularExpression("^[0-9a-fA-F]{64}$")]
    public required string Sha256 { get; init; }
}
