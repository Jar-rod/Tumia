using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tumiia.Domain.Email;

namespace Tumiia.Infrastructure.Email;

/// <summary>Sends email through the Amazon SES v2 API (af-south-1 has no SES SMTP endpoint).</summary>
public sealed class SesEmailSender(IAmazonSimpleEmailServiceV2 ses, IOptions<EmailSettings> options, ILogger<SesEmailSender> logger) : IEmailSender
{
    /// <summary>Sends a plain-text email. The body is never logged, because it can hold a reset link.</summary>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = options.Value.FromAddress,
            Destination = new Destination { ToAddresses = [message.To] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = message.Subject, Charset = "UTF-8" },
                    Body = new Body { Text = new Content { Data = message.Body, Charset = "UTF-8" } },
                },
            },
        };

        await ses.SendEmailAsync(request, cancellationToken);
        logger.LogInformation("Sent email '{Subject}' to {Recipient} through SES", message.Subject, message.To);
    }
}
