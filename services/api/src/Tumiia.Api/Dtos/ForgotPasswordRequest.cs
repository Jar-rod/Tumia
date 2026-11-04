using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>The email to send a reset link to.</summary>
public sealed record ForgotPasswordRequest
{
    /// <summary>The account's email.</summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
