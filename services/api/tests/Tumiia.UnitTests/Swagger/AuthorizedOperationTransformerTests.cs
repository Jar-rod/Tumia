using Microsoft.AspNetCore.Authorization;
using Tumiia.Api.Swagger;

namespace Tumiia.UnitTests.Swagger;

public class AuthorizedOperationTransformerTests
{
    [Fact]
    public void RequiresAuthorisation_WhenEndpointHasAuthorize_ReturnsTrue()
    {
        List<object> metadata = [new AuthorizeAttribute()];

        var requiresAuthorisation = AuthorizedOperationTransformer.RequiresAuthorisation(metadata);

        requiresAuthorisation.ShouldBeTrue();
    }

    [Fact]
    public void RequiresAuthorisation_WhenEndpointAlsoAllowsAnonymous_ReturnsFalse()
    {
        List<object> metadata = [new AuthorizeAttribute(), new AllowAnonymousAttribute()];

        var requiresAuthorisation = AuthorizedOperationTransformer.RequiresAuthorisation(metadata);

        requiresAuthorisation.ShouldBeFalse();
    }

    [Fact]
    public void RequiresAuthorisation_WhenEndpointHasNoAuthorize_ReturnsFalse()
    {
        List<object> metadata = [];

        var requiresAuthorisation = AuthorizedOperationTransformer.RequiresAuthorisation(metadata);

        requiresAuthorisation.ShouldBeFalse();
    }
}
