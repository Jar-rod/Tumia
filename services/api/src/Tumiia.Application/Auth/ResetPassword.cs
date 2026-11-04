using Tumiia.Domain.Audit;

namespace Tumiia.Application.Auth;

/// <summary>Sets a new password from a reset link, then signs the user out everywhere.</summary>
public sealed class ResetPassword(IUserAccounts userAccounts, IRefreshTokenStore refreshTokens, IAuditLog auditLog, TimeProvider clock)
{
    /// <summary>
    /// Changes the password when the email and token are valid. On success every refresh token of the user is
    /// revoked, so anyone who knew the old password is signed out.
    /// </summary>
    public async Task<ResetPasswordResult> ExecuteAsync(string email, string resetToken, string newPassword, string ipAddress, CancellationToken cancellationToken)
    {
        var user = await userAccounts.FindByEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
        {
            await auditLog.RecordAsync(AuditEventType.PasswordReset, null, ipAddress, false, cancellationToken);
            return ResetPasswordResult.BadLink;
        }

        var result = await userAccounts.ResetPasswordAsync(user.Id, resetToken, newPassword, cancellationToken);
        await auditLog.RecordAsync(AuditEventType.PasswordReset, user.Id, ipAddress, result.Succeeded, cancellationToken);
        if (result.Succeeded)
        {
            await refreshTokens.RevokeAllForUserAsync(user.Id, clock.GetUtcNow(), cancellationToken);
        }

        return result;
    }
}
