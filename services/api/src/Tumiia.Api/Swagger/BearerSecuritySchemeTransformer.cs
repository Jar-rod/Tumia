using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tumiia.Api.Swagger;

/// <summary>
/// Adds the JWT bearer security scheme to the OpenAPI document, which makes the "Authorize" button
/// appear in Swagger UI so a token can be pasted in once and sent with every call.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <summary>The name the scheme is registered under in the document.</summary>
    public const string SchemeName = "Bearer";

    /// <summary>Registers the bearer scheme in the document's components.</summary>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the access token returned by the login endpoint.",
        };

        return Task.CompletedTask;
    }
}
