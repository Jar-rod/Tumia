using Tumiia.Domain.Audit;
using Tumiia.Domain.Email;

namespace Tumiia.Application.Auth;

/// <summary>
/// Emails a password reset link if the account exists. The caller is never told whether it does, so this cannot
/// be used to find out which emails have accounts.
/// </summary>
public sealed class RequestPasswordReset(IUserAccounts userAccounts, IEmailSender emailSender, IAuditLog auditLog, AuthSettings settings)
{
    /// <summary>Sends the reset email when the account exists; does nothing visible otherwise.</summary>
    public async Task ExecuteAsync(string email, string ipAddress, CancellationToken cancellationToken)
    {
        var user = await userAccounts.FindByEmailAsync(email.Trim(), cancellationToken);
        await auditLog.RecordAsync(AuditEventType.PasswordResetRequested, user?.Id, ipAddress, user is not null, cancellationToken);
        if (user is null)
        {
            return;
        }

        var resetToken = await userAccounts.CreatePasswordResetTokenAsync(user.Id, cancellationToken);
        await emailSender.SendAsync(BuildEmail(user, resetToken), cancellationToken);
    }

    /// <summary>The link opens the app's reset page with the email and the one-hour token in the address.</summary>
    public string BuildResetLink(string email, string resetToken)
    {
        var baseUrl = settings.AppBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";
    }

    private EmailMessage BuildEmail(UserAccount user, string resetToken)
    {
        var body = $"""
            Hi {user.DisplayName},

            Someone asked to reset the password for your TUMIIA account. To choose a new password, open this link
            within one hour:

            {BuildResetLink(user.Email, resetToken)}

            If this wasn't you, you can ignore this email; your password has not changed.
            """;

        return new EmailMessage(user.Email, "Reset your TUMIIA password", body);
    }
}
