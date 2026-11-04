using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Tumiia.Api.Security;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RateLimitAndOriginTests(TumiiaApiFactory factory)
{
    private const string OriginSecret = "test-origin-secret";

    [Fact]
    public async Task Login_TheSixthAttemptInAMinute_Answers429WithRetryAfter()
    {
        using var realLimits = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "5"));
        var auth = new AuthClient(realLimits.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            (await auth.LoginAsync(AuthClient.NewEmail())).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var sixth = await auth.LoginAsync(AuthClient.NewEmail());

        sixth.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        int.Parse(sixth.Headers.GetValues("Retry-After").Single()).ShouldBeGreaterThan(0);
        var problem = await sixth.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problem!.Status.ShouldBe(429);
    }

    /// <summary>The API as it runs in AWS: Production, with the CloudFront secret configured.</summary>
    private WebApplicationFactory<Program> AsInAws()
    {
        _ = factory.CreateClient();
        return factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("OriginVerify:Secret", OriginSecret)
            .UseSetting("App:BaseUrl", "https://app.tumiia.test")
            .UseSetting("Jwt:SigningKey", new string('k', 48)));
    }

    [Fact]
    public async Task InAws_ARequestThatSkippedCloudFront_Answers403()
    {
        using var api = AsInAws();

        var response = await api.CreateClient().GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InAws_ARequestFromCloudFront_PassesTheOriginCheck()
    {
        using var api = AsInAws();
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add(OriginVerifyMiddleware.HeaderName, OriginSecret);

        var response = await client.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InAws_TheLoadBalancerHealthCheck_NeedsNoOriginHeader()
    {
        using var api = AsInAws();

        var response = await api.CreateClient().GetAsync("/api/v1/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
