namespace Tumiia.Domain.Email;

/// <summary>A plain-text email to one person.</summary>
public sealed record EmailMessage(string To, string Subject, string Body);
