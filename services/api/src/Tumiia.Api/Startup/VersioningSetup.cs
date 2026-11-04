using Asp.Versioning;

namespace Tumiia.Api.Startup;

/// <summary>
/// Sets up API versioning with the version in the URL (<c>/api/v1/...</c>), and one OpenAPI document per version.
/// </summary>
public static class VersioningSetup
{
    /// <summary>
    /// Version 1.0 is the default. Responses carry an <c>api-supported-versions</c> header so callers can see
    /// which versions exist. Endpoints are grouped by version ("v1"), and each group becomes its own OpenAPI
    /// document at <c>/openapi/v1.json</c>.
    /// </summary>
    public static IServiceCollection AddTumiiaApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options => OpenApiSetup.ConfigureDocument(options.Document));

        return services;
    }
}
