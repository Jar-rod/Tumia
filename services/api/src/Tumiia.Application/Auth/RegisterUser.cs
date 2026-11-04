using Tumiia.Domain.Audit;

namespace Tumiia.Application.Auth;

/// <summary>Creates an account and signs the new user straight in (no email confirmation in this version).</summary>
public sealed class RegisterUser(IUserAccounts userAccounts, SessionIssuer sessions, IAuditLog auditLog)
{
    /// <summary>Registers the user. On success, returns a session; otherwise says why it failed.</summary>
    public async Task<RegisterResult> ExecuteAsync(string email, string password, string displayName, string ipAddress, CancellationToken cancellationToken)
    {
        var created = await userAccounts.CreateAsync(email.Trim(), password, displayName.Trim(), cancellationToken);
        if (created.User is null)
        {
            return new RegisterResult(null, created.DuplicateEmail, created.Errors);
        }

        await auditLog.RecordAsync(AuditEventType.Registered, created.User.Id, ipAddress, true, cancellationToken);
        var session = await sessions.StartAsync(created.User, cancellationToken);
        return new RegisterResult(session, false, created.Errors);
    }
}
