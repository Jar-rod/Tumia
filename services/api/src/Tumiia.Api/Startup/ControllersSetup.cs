using Microsoft.AspNetCore.Mvc.Formatters;

namespace Tumiia.Api.Startup;

/// <summary>Sets up MVC controllers so every response is JSON.</summary>
public static class ControllersSetup
{
    /// <summary>
    /// Adds controllers and removes the plain-text and <c>text/json</c> output formats, so the API (and the
    /// OpenAPI contract the frontend client is generated from) only offers <c>application/json</c>.
    /// Error responses keep <c>application/problem+json</c>.
    /// </summary>
    public static IServiceCollection AddTumiiaControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            foreach (var jsonFormatter in options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>())
            {
                jsonFormatter.SupportedMediaTypes.Remove("text/json");
            }
        });

        return services;
    }
}
