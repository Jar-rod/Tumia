namespace Tumiia.Api.Startup;

/// <summary>
/// Lets the local Vite dev server call the API. Used in Development only: in AWS, CloudFront serves the
/// app and the API from one origin, so no CORS is needed there.
/// </summary>
public static class CorsSetup
{
    /// <summary>The name of the CORS policy for the local frontend.</summary>
    public const string LocalFrontendPolicy = "LocalFrontend";

    /// <summary>The address <c>npm run dev</c> serves the React app on.</summary>
    public const string LocalFrontendOrigin = "http://localhost:5173";

    /// <summary>Registers the local frontend policy. Credentials are allowed so the refresh cookie works.</summary>
    public static IServiceCollection AddTumiiaCors(this IServiceCollection services)
    {
        services.AddCors(options => options.AddPolicy(LocalFrontendPolicy, policy => policy
            .WithOrigins(LocalFrontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return services;
    }
}
