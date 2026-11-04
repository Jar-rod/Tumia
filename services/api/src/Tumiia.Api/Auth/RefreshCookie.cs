namespace Tumiia.Api.Auth;

/// <summary>
/// The refresh token cookie. JavaScript cannot read it (HttpOnly), it only travels over HTTPS (Secure), never to
/// other sites (SameSite=Strict), and only to the auth endpoints (Path).
/// </summary>
public static class RefreshCookie
{
    /// <summary>The cookie's name.</summary>
    public const string Name = "tumiia_refresh";

    /// <summary>The cookie is only sent to these endpoints.</summary>
    public const string CookiePath = "/api/v1/auth";

    /// <summary>Sets the cookie to a new refresh token.</summary>
    public static void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAt)
    {
        response.Cookies.Append(Name, refreshToken, Options(expiresAt));
    }

    /// <summary>The refresh token the browser sent, or null.</summary>
    public static string? Read(HttpRequest request)
    {
        return request.Cookies[Name];
    }

    /// <summary>Tells the browser to delete the cookie.</summary>
    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(Name, Options(null));
    }

    private static CookieOptions Options(DateTimeOffset? expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expiresAt,
            IsEssential = true,
        };
    }
}
