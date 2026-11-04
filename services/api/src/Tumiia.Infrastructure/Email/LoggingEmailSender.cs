using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tumiia.Domain.Email;

namespace Tumiia.Infrastructure.Email;

/// <summary>
/// The local stand-in for SES. Nothing is sent: the log says an email was "sent" and to whom (never the link),
/// and the full email is written to a file under <c>.dev-emails/</c> so a developer can open a reset link.
/// </summary>
public sealed class LoggingEmailSender(IHostEnvironment environment, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <summary>The folder, inside the API project, where local emails are written. Ignored by git.</summary>
    public const string FolderName = ".dev-emails";

    /// <summary>Writes the email to a file and logs where it is.</summary>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(environment.ContentRootPath, FolderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.txt");

        var content = $"To: {message.To}\nSubject: {message.Subject}\n\n{message.Body}\n";
        await File.WriteAllTextAsync(path, content, cancellationToken);

        logger.LogInformation("Email '{Subject}' to {Recipient} was not sent (local sender); it was written to {Path}", message.Subject, message.To, path);
    }
}
