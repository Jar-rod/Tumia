using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Tumiia.Api.Startup;

/// <summary>Sets up MVC controllers: every action needs a signed-in user unless marked anonymous, and every response is JSON.</summary>
public static class ControllersSetup
{
    /// <summary>
    /// Adds controllers. Every action needs a signed-in user unless it has <c>[AllowAnonymous]</c>, so a new endpoint
    /// is closed until someone decides otherwise. (This applies to controller actions only, so an unknown address
    /// still answers 404.) The plain-text and <c>text/json</c> output formats are removed, so the API and its
    /// OpenAPI contract only offer <c>application/json</c>; errors keep <c>application/problem+json</c>.
    /// </summary>
    public static IServiceCollection AddTumiiaControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(new AuthorizeFilter());
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            foreach (var jsonFormatter in options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>())
            {
                jsonFormatter.SupportedMediaTypes.Remove("text/json");
            }
        });

        return services;
    }
}
