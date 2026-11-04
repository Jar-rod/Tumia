using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>A new account.</summary>
public sealed record RegisterRequest
{
    /// <summary>The email to sign in with.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; init; }

    /// <summary>At least 10 characters, with an upper-case letter, a lower-case letter, a digit and a symbol.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>The name shown in the app.</summary>
    [Required]
    [MaxLength(100)]
    public required string DisplayName { get; init; }
}
