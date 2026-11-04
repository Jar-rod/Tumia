using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;
using Tumiia.Api.Auth;
using Tumiia.Api.Logging;
using Tumiia.Api.RateLimiting;

namespace Tumiia.UnitTests.Security;

public class RateLimitingAndCookieTests
{
    [Fact]
    public void UserOrIpKey_WhenSignedIn_CountsByUser()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u-1")], "test")) };

        var key = RateLimitingSetup.UserOrIpKey(context);

        key.ShouldBe("user:u-1");
    }

    [Fact]
    public void UserOrIpKey_WhenSignedOut_CountsByIpAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        var key = RateLimitingSetup.UserOrIpKey(context);

        key.ShouldBe("ip:10.0.0.5");
    }

    [Fact]
    public void Enrich_WhenTheRateLimiterRejectedTheRequest_LogsIt()
    {
        var diagnosticContext = Substitute.For<IDiagnosticContext>();
        var context = new DefaultHttpContext();
        context.Items[RequestLogEnricher.RateLimitedItemKey] = true;

        RequestLogEnricher.Enrich(diagnosticContext, context);

        diagnosticContext.Received().Set("RateLimited", true);
    }

    [Fact]
    public void RefreshCookie_WhenWritten_IsHttpOnlySecureStrictAndLimitedToTheAuthPath()
    {
        var context = new DefaultHttpContext();

        RefreshCookie.Write(context.Response, "token-value", DateTimeOffset.UtcNow.AddDays(14));

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        header.ShouldStartWith("tumiia_refresh=token-value");
        header.ShouldContain("httponly");
        header.ShouldContain("secure");
        header.ShouldContain("samesite=strict");
        header.ShouldContain("path=/api/v1/auth");
    }
}
