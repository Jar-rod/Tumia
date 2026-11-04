using Tumiia.Api.ErrorHandling;
using Tumiia.Api.Logging;

namespace Tumiia.Api.Startup;

/// <summary>Makes every error response a ProblemDetails (RFC 7807) body.</summary>
public static class ErrorHandlingSetup
{
    /// <summary>
    /// Registers ProblemDetails and the global exception handler. Every problem response also carries the
    /// request's correlation id, so a user's error report can be matched to the logs.
    /// </summary>
    public static IServiceCollection AddTumiiaErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId))
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
