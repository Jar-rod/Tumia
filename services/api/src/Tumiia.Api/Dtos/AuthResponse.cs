namespace Tumiia.Api.Dtos;

/// <summary>
/// A signed-in session. The access token goes in the <c>Authorization: Bearer</c> header and should be kept in
/// memory only. The refresh token is not in the body: it is set as an HttpOnly cookie.
/// </summary>
/// <param name="AccessToken">The JWT to send with each API call. Valid for 15 minutes.</param>
/// <param name="ExpiresAt">When the access token stops working (UTC).</param>
/// <param name="User">Who is signed in.</param>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserResponse User);
