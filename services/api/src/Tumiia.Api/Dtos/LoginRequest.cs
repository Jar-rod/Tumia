using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>Email and password.</summary>
public sealed record LoginRequest
{
    /// <summary>The account's email.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>The account's password.</summary>
    [Required]
    public required string Password { get; init; }
}
