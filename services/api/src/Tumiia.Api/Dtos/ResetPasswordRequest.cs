using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>A new password, with the email and token from the reset link.</summary>
public sealed record ResetPasswordRequest
{
    /// <summary>The <c>email</c> from the reset link.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>The <c>token</c> from the reset link.</summary>
    [Required]
    public required string Token { get; init; }

    /// <summary>The new password. Same rules as on registration.</summary>
    [Required]
    public required string NewPassword { get; init; }
}
