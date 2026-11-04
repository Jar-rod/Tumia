using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using NSubstitute;
using Serilog;
using Tumiia.Api.Logging;

namespace Tumiia.UnitTests.Logging;

public class RequestLogEnricherTests
{
    [Fact]
    public void GetUserId_WhenNotSignedIn_ReturnsAnonymous()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var userId = RequestLogEnricher.GetUserId(user);

        userId.ShouldBe("anonymous");
    }

    [Fact]
    public void GetUserId_WhenSignedIn_ReturnsSubjectClaim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-42")], "test"));

        var userId = RequestLogEnricher.GetUserId(user);

        userId.ShouldBe("user-42");
    }

    [Fact]
    public void GetRouteTemplate_WhenNoRouteMatched_ReturnsUnmatched()
    {
        var context = new DefaultHttpContext();

        var template = RequestLogEnricher.GetRouteTemplate(context);

        template.ShouldBe("unmatched");
    }

    [Fact]
    public void GetRouteTemplate_WhenRouteMatched_ReturnsTemplateNotRawPath()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/documents/3f2a";
        context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("api/v{version}/documents/{id}"), 0, null, null));

        var template = RequestLogEnricher.GetRouteTemplate(context);

        template.ShouldBe("api/v{version}/documents/{id}");
    }

    [Fact]
    public void Enrich_WhenCalled_SetsRouteVersionUserAndClientIp()
    {
        var diagnosticContext = Substitute.For<IDiagnosticContext>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.7");

        RequestLogEnricher.Enrich(diagnosticContext, context);

        diagnosticContext.Received().Set("RouteTemplate", "unmatched");
        diagnosticContext.Received().Set("ApiVersion", "none");
        diagnosticContext.Received().Set("UserId", "anonymous");
        diagnosticContext.Received().Set("ClientIp", "10.0.0.7");
    }
}
