namespace Tumiia.Api.RateLimiting;

/// <summary>Names for <c>[EnableRateLimiting(...)]</c>. The default limit applies to every request without naming it.</summary>
public static class RateLimitPolicies
{
    /// <summary>Login, register, refresh, forgot and reset password: 5 per minute per client IP.</summary>
    public const string Auth = "auth";

    /// <summary>The upload-request endpoint: 30 per minute per user.</summary>
    public const string Upload = "upload";
}
