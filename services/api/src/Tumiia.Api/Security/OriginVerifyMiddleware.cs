using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Tumiia.Api.Security;

/// <summary>
/// Outside Development, rejects (403) every request that did not come through CloudFront, which adds a secret
/// <c>X-Origin-Verify</c> header. The health check is the one exception: the load balancer calls it directly.
/// A request that passes is marked as verified, so <see cref="ClientIpResolver"/> can trust CloudFront's headers.
/// </summary>
public sealed class OriginVerifyMiddleware(
    RequestDelegate next,
    IOptions<OriginVerifySettings> options,
    IHostEnvironment environment,
    ILogger<OriginVerifyMiddleware> logger)
{
    /// <summary>The header CloudFront adds.</summary>
    public const string HeaderName = "X-Origin-Verify";

    /// <summary>Set in <see cref="HttpContext.Items"/> when the request carried the right secret.</summary>
    public const string VerifiedItemKey = "OriginVerified";

    /// <summary>The load balancer's health check path, which never goes through CloudFront.</summary>
    public static readonly PathString HealthCheckPath = new("/api/v1/health");

    /// <summary>Lets the request through, or answers 403 with a ProblemDetails body.</summary>
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        if (environment.IsDevelopment())
        {
            await next(context);
            return;
        }

        var secret = options.Value.Secret;
        if (IsValid(context.Request.Headers[HeaderName].ToString(), secret))
        {
            context.Items[VerifiedItemKey] = true;
            await next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments(HealthCheckPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrEmpty(secret))
        {
            logger.LogError("OriginVerify:Secret is not set, so every request except the health check is rejected");
        }

        await RejectAsync(context, problemDetails);
    }

    /// <summary>
    /// True when the presented value equals the secret. Compared in constant time, so the answer time does not
    /// reveal how much of a guess was right. An empty secret never matches.
    /// </summary>
    public static bool IsValid(string? presented, string? secret)
    {
        if (string.IsNullOrEmpty(presented) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(secret));
    }

    private static async Task RejectAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "Requests must come through the TUMIIA web address.",
            },
        });
    }
}
