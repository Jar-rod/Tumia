namespace Tumiia.Application.Auth;

/// <summary>A signed, short-lived access token (JWT) and when it expires.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
