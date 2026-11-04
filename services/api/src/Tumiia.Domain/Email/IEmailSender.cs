namespace Tumiia.Domain.Email;

/// <summary>Sends email. In AWS this is Amazon SES; locally, a sender that only logs and writes to a file.</summary>
public interface IEmailSender
{
    /// <summary>Sends one email.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
