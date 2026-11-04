using System.Collections.Concurrent;
using Tumiia.Domain.Email;

namespace Tumiia.IntegrationTests;

/// <summary>Stands in for SES in tests: keeps every email so a test can read the reset link.</summary>
public sealed class CapturedEmails : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    /// <summary>Every email "sent" so far.</summary>
    public IReadOnlyList<EmailMessage> Sent => [.. _sent];

    /// <summary>Keeps the email instead of sending it.</summary>
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
