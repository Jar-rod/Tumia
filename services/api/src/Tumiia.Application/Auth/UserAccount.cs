namespace Tumiia.Application.Auth;

/// <summary>What the rest of the app needs to know about a user.</summary>
public sealed record UserAccount(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
