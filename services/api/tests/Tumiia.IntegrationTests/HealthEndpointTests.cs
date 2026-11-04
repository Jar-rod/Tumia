using System.Net;
using System.Net.Http.Json;
using Tumiia.Api.Dtos;
using Tumiia.Api.Logging;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(TumiiaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_WhenDatabaseIsReachable_Returns200Healthy()
    {
        var response = await _client.GetAsync("/api/v1/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        body!.Status.ShouldBe("Healthy");
        body.Checks.ShouldContain(new HealthCheckEntry("database", "Healthy"));
    }

    [Fact]
    public async Task GetHealth_WhenCalled_ReportsSupportedApiVersionsInHeader()
    {
        var response = await _client.GetAsync("/api/v1/health", TestContext.Current.CancellationToken);

        response.Headers.GetValues("api-supported-versions").ShouldContain("1.0");
    }

    [Fact]
    public async Task GetHealth_WhenCorrelationIdIsSent_EchoesItBack()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-run-42");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).ShouldBe(["test-run-42"]);
    }

    [Fact]
    public async Task UnknownRoute_WhenRequested_ReturnsProblemDetailsWithCorrelationId()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"correlationId\"");
    }

    [Fact]
    public async Task Preflight_WhenFromLocalViteDevServer_IsAllowedInDevelopment()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["http://localhost:5173"]);
    }
}
