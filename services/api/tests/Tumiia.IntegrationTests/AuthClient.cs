using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tumiia.Api.Auth;
using Tumiia.Api.Dtos;

namespace Tumiia.IntegrationTests;

/// <summary>Small helpers that call the auth endpoints like a browser would, managing the refresh cookie by hand.</summary>
public sealed class AuthClient(HttpClient client)
{
    public const string GoodPassword = "Bricks-R18450";

    /// <summary>A new, unused email for each test, so tests sharing the database never collide.</summary>
    public static string NewEmail() => $"user-{Guid.NewGuid():N}@tumiia.test";

    public Task<HttpResponseMessage> RegisterAsync(string email, string password = GoodPassword, string displayName = "Test User")
    {
        return client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, displayName }, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password = GoodPassword)
    {
        return client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> RefreshAsync(string? refreshCookie)
    {
        return SendAsync(HttpMethod.Post, "/api/v1/auth/refresh", accessToken: null, refreshCookie);
    }

    public Task<HttpResponseMessage> LogoutAsync(string accessToken, string? refreshCookie)
    {
        return SendAsync(HttpMethod.Post, "/api/v1/auth/logout", accessToken, refreshCookie);
    }

    public Task<HttpResponseMessage> MeAsync(string? accessToken)
    {
        return SendAsync(HttpMethod.Get, "/api/v1/auth/me", accessToken, refreshCookie: null);
    }

    /// <summary>Registers a new user and returns their session and refresh cookie.</summary>
    public async Task<(AuthResponse Session, string RefreshCookie, string Email)> RegisterNewUserAsync()
    {
        var email = NewEmail();
        var response = await RegisterAsync(email);
        response.EnsureSuccessStatusCode();
        return (await ReadSessionAsync(response), RefreshCookieFrom(response)!, email);
    }

    public static async Task<AuthResponse> ReadSessionAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!;
    }

    /// <summary>The refresh token value from the response's Set-Cookie header, or null (an empty value means "deleted").</summary>
    public static string? RefreshCookieFrom(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{RefreshCookie.Name}=";
        var cookie = cookies.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        return cookie?[prefix.Length..].Split(';')[0];
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? accessToken, string? refreshCookie)
    {
        var request = new HttpRequestMessage(method, path);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (refreshCookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookie.Name}={refreshCookie}");
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
