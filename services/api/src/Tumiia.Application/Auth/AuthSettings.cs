namespace Tumiia.Application.Auth;

/// <summary>Settings the sign-in use cases need, read from configuration by the API.</summary>
public sealed class AuthSettings
{
    /// <summary>How long a refresh token (and so a sign-in) lasts without being used. 14 days.</summary>
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);

    /// <summary>The app's address, for links in emails, for example <c>https://app.example.co.za</c>.</summary>
    public required string AppBaseUrl { get; init; }

    /// <summary>Emails that get the Admin role when they register or log in (<c>Admin:Emails</c>).</summary>
    public IReadOnlySet<string> AdminEmails { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
