using System.Security.Claims;

namespace Tumiia.Api.Auth;

/// <summary>Reads the signed-in user's id from the access token.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The user id from the token's <c>sub</c> claim. Only call this on endpoints that need a signed-in user.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirstValue("sub")!);
    }
}
