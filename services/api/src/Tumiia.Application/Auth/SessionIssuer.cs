using Tumiia.Domain.Auth;

namespace Tumiia.Application.Auth;

/// <summary>Starts a new sign-in: gives admins their role, then creates an access token and a new refresh token family.</summary>
public sealed class SessionIssuer(
    IUserAccounts userAccounts,
    IRefreshTokenStore refreshTokens,
    IAccessTokenIssuer accessTokens,
    AuthSettings settings,
    TimeProvider clock)
{
    /// <summary>Starts a sign-in for a user whose identity has already been checked.</summary>
    public async Task<AuthSession> StartAsync(UserAccount user, CancellationToken cancellationToken)
    {
        user = await EnsureAdminRoleAsync(user, cancellationToken);

        var secret = RefreshTokenSecret.Generate();
        var token = RefreshToken.StartFamily(user.Id, secret.Hash, clock.GetUtcNow(), settings.RefreshTokenLifetime);
        await refreshTokens.AddAsync(token, cancellationToken);

        return new AuthSession(accessTokens.Issue(user), secret, user);
    }

    /// <summary>
    /// Gives the Admin role to anyone whose email is listed in <c>Admin:Emails</c>, so admins can be added by
    /// configuration alone. Returns the user with up-to-date roles.
    /// </summary>
    private async Task<UserAccount> EnsureAdminRoleAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var shouldBeAdmin = settings.AdminEmails.Contains(user.Email);
        if (!shouldBeAdmin || user.Roles.Contains(Roles.Admin))
        {
            return user;
        }

        await userAccounts.AddToRoleAsync(user.Id, Roles.Admin, cancellationToken);
        return user with { Roles = [.. user.Roles, Roles.Admin] };
    }
}
