using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tumiia.Api.Logging;
using Tumiia.Api.Security;

namespace Tumiia.Api.RateLimiting;

/// <summary>
/// Sets up the built-in ASP.NET Core rate limiter. Limits are read from configuration on each request, so they can
/// be changed without a code change. A rejected request gets 429 with a <c>Retry-After</c> header and a
/// ProblemDetails body.
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>Registers the default limit for every request, and the named "auth" and "upload" policies.</summary>
    public static IServiceCollection AddTumiiaRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(DefaultPartition);
            options.AddPolicy(RateLimitPolicies.Auth, AuthPartition);
            options.AddPolicy(RateLimitPolicies.Upload, UploadPartition);
            options.OnRejected = RejectAsync;
        });

        return services;
    }

    /// <summary>The key a caller is counted under: their user id when signed in, otherwise their IP address.</summary>
    public static string UserOrIpKey(HttpContext context)
    {
        var userId = RequestLogEnricher.GetUserId(context.User);
        return userId == "anonymous" ? $"ip:{ClientIpResolver.Resolve(context)}" : $"user:{userId}";
    }

    private static RateLimitSettings Settings(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitSettings>>().CurrentValue;
    }

    // A token bucket refills steadily, so a busy user is slowed down rather than locked out for a whole minute.
    private static RateLimitPartition<string> DefaultPartition(HttpContext context)
    {
        var limit = Settings(context).Default;
        return RateLimitPartition.GetTokenBucketLimiter(UserOrIpKey(context), _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = limit.PermitLimit,
            TokensPerPeriod = limit.PermitLimit,
            ReplenishmentPeriod = limit.Window,
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    private static RateLimitPartition<string> AuthPartition(HttpContext context)
    {
        return FixedWindow($"ip:{ClientIpResolver.Resolve(context)}", Settings(context).Auth);
    }

    private static RateLimitPartition<string> UploadPartition(HttpContext context)
    {
        return FixedWindow(UserOrIpKey(context), Settings(context).Upload);
    }

    private static RateLimitPartition<string> FixedWindow(string key, RateLimit limit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit.PermitLimit,
            Window = limit.Window,
            QueueLimit = 0,
        });
    }

    /// <summary>Answers 429 with how long to wait, and marks the request so the request log shows it was rate limited.</summary>
    private static async ValueTask RejectAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        var context = rejected.HttpContext;
        context.Items[RequestLogEnricher.RateLimitedItemKey] = true;
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var waitSeconds = 60;
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            waitSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.Response.Headers.RetryAfter = waitSeconds.ToString(CultureInfo.InvariantCulture);
        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = $"Please wait {waitSeconds} seconds and try again.",
            },
        });
    }
}
