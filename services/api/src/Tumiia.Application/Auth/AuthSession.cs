using Tumiia.Domain.Auth;

namespace Tumiia.Application.Auth;

/// <summary>What a successful register, login or refresh hands back: an access token, a refresh token and the user.</summary>
/// <param name="AccessToken">The short-lived JWT, returned in the response body.</param>
/// <param name="RefreshToken">The secret goes in the refresh cookie; only its hash is stored.</param>
/// <param name="User">The signed-in user.</param>
public sealed record AuthSession(AccessToken AccessToken, RefreshTokenSecret RefreshToken, UserAccount User);
