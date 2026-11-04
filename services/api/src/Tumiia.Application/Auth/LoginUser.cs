using Tumiia.Domain.Audit;

namespace Tumiia.Application.Auth;

/// <summary>
/// Checks an email and password and starts a session. Every failure (unknown email, wrong password, locked
/// account) looks the same to the caller, so the login cannot be used to find out which emails have accounts.
/// </summary>
public sealed class LoginUser(IUserAccounts userAccounts, SessionIssuer sessions, IAuditLog auditLog)
{
    /// <summary>Returns a session when the email and password are right and the account is not locked; otherwise null.</summary>
    public async Task<AuthSession?> ExecuteAsync(string email, string password, string ipAddress, CancellationToken cancellationToken)
    {
        var user = await userAccounts.FindByEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
        {
            await auditLog.RecordAsync(AuditEventType.LoginFailed, null, ipAddress, false, cancellationToken);
            return null;
        }

        var check = await userAccounts.CheckPasswordAsync(user.Id, password, cancellationToken);
        if (check != PasswordCheckResult.Succeeded)
        {
            await RecordFailureAsync(check, user.Id, ipAddress, cancellationToken);
            return null;
        }

        await auditLog.RecordAsync(AuditEventType.LoginSucceeded, user.Id, ipAddress, true, cancellationToken);
        return await sessions.StartAsync(user, cancellationToken);
    }

    // A locked account gets a LockedOut event; the attempt that causes the lock gets both events.
    private async Task RecordFailureAsync(PasswordCheckResult check, Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        if (check != PasswordCheckResult.LockedOut)
        {
            await auditLog.RecordAsync(AuditEventType.LoginFailed, userId, ipAddress, false, cancellationToken);
        }

        if (check is PasswordCheckResult.LockedOut or PasswordCheckResult.JustLockedOut)
        {
            await auditLog.RecordAsync(AuditEventType.LockedOut, userId, ipAddress, false, cancellationToken);
        }
    }
}
