using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tumiia.Api.Swagger;

/// <summary>
/// Marks each endpoint that needs a signed-in user as using the bearer scheme, so Swagger UI sends the
/// token for those calls and shows a padlock. Anonymous endpoints are left alone.
/// </summary>
public sealed class AuthorizedOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>Adds the bearer requirement to the operation when the endpoint requires authorisation.</summary>
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (!RequiresAuthorisation(context.Description.ActionDescriptor.EndpointMetadata))
        {
            return Task.CompletedTask;
        }

        var scheme = new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document);
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });
        return Task.CompletedTask;
    }

    /// <summary>True when the endpoint has [Authorize] (directly or from its controller) and no [AllowAnonymous].</summary>
    public static bool RequiresAuthorisation(IList<object> endpointMetadata)
    {
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();
        var allowsAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        return hasAuthorize && !allowsAnonymous;
    }
}
