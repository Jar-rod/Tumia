using Tumiia.Domain.Audit;
using Tumiia.Domain.Auth;

namespace Tumiia.Application.Auth;

/// <summary>Ends the current sign-in by revoking its refresh token family.</summary>
public sealed class LogoutUser(IRefreshTokenStore refreshTokens, IAuditLog auditLog, TimeProvider clock)
{
    /// <summary>
    /// Revokes the sign-in that the refresh token belongs to, if the token belongs to this user. Logging out always
    /// succeeds from the caller's point of view.
    /// </summary>
    public async Task ExecuteAsync(Guid userId, string? refreshTokenValue, string ipAddress, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var token = await refreshTokens.FindByHashAsync(RefreshTokenSecret.HashOf(refreshTokenValue), cancellationToken);
            if (token is not null && token.UserId == userId)
            {
                await refreshTokens.RevokeFamilyAsync(token.FamilyId, clock.GetUtcNow(), cancellationToken);
            }
        }

        await auditLog.RecordAsync(AuditEventType.LoggedOut, userId, ipAddress, true, cancellationToken);
    }
}
