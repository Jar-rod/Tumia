namespace Tumiia.Api.RateLimiting;

/// <summary>One limit: at most <see cref="PermitLimit"/> requests every <see cref="WindowSeconds"/> seconds.</summary>
public sealed class RateLimit
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    /// <summary>The window as a time span.</summary>
    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);
}
