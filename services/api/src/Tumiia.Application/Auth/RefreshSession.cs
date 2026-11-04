using Tumiia.Domain.Audit;
using Tumiia.Domain.Auth;

namespace Tumiia.Application.Auth;

/// <summary>
/// Swaps a refresh token for a new access token and a new refresh token (rotation). If the token was already used,
/// it may have been stolen, so the whole sign-in is ended.
/// </summary>
public sealed class RefreshSession(
    IRefreshTokenStore refreshTokens,
    IUserAccounts userAccounts,
    IAccessTokenIssuer accessTokens,
    IAuditLog auditLog,
    AuthSettings settings,
    TimeProvider clock)
{
    /// <summary>Returns a new session, or null when the token is unknown, expired, revoked or reused.</summary>
    public async Task<AuthSession?> ExecuteAsync(string? refreshTokenValue, string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return null;
        }

        var token = await refreshTokens.FindByHashAsync(RefreshTokenSecret.HashOf(refreshTokenValue), cancellationToken);
        if (token is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();
        if (token.IsReuse())
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            await auditLog.RecordAsync(AuditEventType.RefreshTokenReuseDetected, token.UserId, ipAddress, false, cancellationToken);
            return null;
        }

        if (!token.IsActive(now))
        {
            return null;
        }

        return await RotateAsync(token, now, cancellationToken);
    }

    private async Task<AuthSession?> RotateAsync(RefreshToken token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var user = await userAccounts.FindByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var secret = RefreshTokenSecret.Generate();
        var replacement = token.Rotate(secret.Hash, now, settings.RefreshTokenLifetime);
        await refreshTokens.SaveRotationAsync(token, replacement, cancellationToken);

        return new AuthSession(accessTokens.Issue(user), secret, user);
    }
}
