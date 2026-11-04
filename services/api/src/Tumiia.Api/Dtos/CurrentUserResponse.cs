namespace Tumiia.Api.Dtos;

/// <summary>The signed-in user.</summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="Roles">"User", and "Admin" for administrators.</param>
public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
