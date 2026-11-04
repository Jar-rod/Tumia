using Microsoft.AspNetCore.OpenApi;
using Tumiia.Api.Swagger;

namespace Tumiia.Api.Startup;

/// <summary>Configures what goes into each OpenAPI document, and serves the documents and Swagger UI.</summary>
public static class OpenApiSetup
{
    /// <summary>The title shown at the top of every OpenAPI document and the Swagger UI page.</summary>
    public const string Title = "TUMIIA API";

    /// <summary>The description shown under the title.</summary>
    public const string Description = "Intelligent filing for construction project documents.";

    /// <summary>
    /// Applied to every version's document: sets the title and description, and adds the JWT bearer scheme, so Swagger UI
    /// gets an "Authorize" button and sends the token on endpoints that need it.
    /// </summary>
    public static void ConfigureDocument(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = Title;
            document.Info.Description = Description;
            return Task.CompletedTask;
        });
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddOperationTransformer<AuthorizedOperationTransformer>();
    }

    /// <summary>
    /// True when the OpenAPI JSON and Swagger UI should be served. Controlled by <c>Swagger:Enabled</c>;
    /// when it is not set, on in Development and off everywhere else.
    /// </summary>
    public static bool IsSwaggerEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        return configuration.GetValue("Swagger:Enabled", environment.IsDevelopment());
    }

    /// <summary>
    /// Serves the Swagger UI page at <c>/swagger</c>. Call this before authentication, so the page itself does not
    /// need a token (the "Authorize" button adds one to the calls it makes).
    /// </summary>
    public static WebApplication UseTumiiaSwaggerUi(this WebApplication app)
    {
        var versions = app.DescribeApiVersions();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            foreach (var version in versions)
            {
                options.SwaggerEndpoint($"/openapi/{version.GroupName}.json", $"{Title} {version.GroupName}");
            }
        });

        return app;
    }

    /// <summary>Serves <c>/openapi/{version}.json</c> for every API version, without needing a token.</summary>
    public static WebApplication MapTumiiaOpenApi(this WebApplication app)
    {
        app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
        return app;
    }
}
