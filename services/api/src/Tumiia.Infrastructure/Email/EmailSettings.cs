namespace Tumiia.Infrastructure.Email;

/// <summary>Email settings, from the <c>Email</c> configuration section.</summary>
public sealed class EmailSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Email";

    /// <summary>"Ses" sends through Amazon SES; anything else (the default) only logs and writes to a local file.</summary>
    public string Provider { get; set; } = "Logging";

    /// <summary>The sender address, for example <c>no-reply@app.example.co.za</c>.</summary>
    public string FromAddress { get; set; } = "no-reply@localhost";

    /// <summary>True when emails really go out through SES.</summary>
    public bool UsesSes => string.Equals(Provider, "Ses", StringComparison.OrdinalIgnoreCase);
}
