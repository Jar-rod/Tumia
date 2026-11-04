namespace Tumiia.Api.RateLimiting;

/// <summary>The rate limits, from the <c>RateLimiting</c> configuration section.</summary>
public sealed class RateLimitSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Sign-in endpoints, per client IP. 5 per minute.</summary>
    public RateLimit Auth { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };

    /// <summary>Upload requests, per user. 30 per minute.</summary>
    public RateLimit Upload { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    /// <summary>Everything, per user (or per IP when signed out). 120 per minute.</summary>
    public RateLimit Default { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };
}
