using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Tumiia.Api.Controllers;
using Tumiia.Api.Dtos;

namespace Tumiia.UnitTests.Controllers;

public class HealthControllerTests
{
    private static HealthCheckService ServiceReturning(HealthStatus databaseStatus)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(databaseStatus, null, TimeSpan.FromMilliseconds(5), null, null),
        };
        var service = Substitute.For<HealthCheckService>();
        service.CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(new HealthReport(entries, TimeSpan.FromMilliseconds(5)));
        return service;
    }

    [Fact]
    public async Task GetAsync_WhenEveryCheckPasses_Returns200Healthy()
    {
        var controller = new HealthController(ServiceReturning(HealthStatus.Healthy));

        var result = await controller.GetAsync(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var body = ok.Value.ShouldBeOfType<HealthResponse>();
        body.Status.ShouldBe("Healthy");
        body.Checks.ShouldHaveSingleItem().ShouldBe(new HealthCheckEntry("database", "Healthy"));
    }

    [Fact]
    public async Task GetAsync_WhenDatabaseIsDown_Returns503Unhealthy()
    {
        var controller = new HealthController(ServiceReturning(HealthStatus.Unhealthy));

        var result = await controller.GetAsync(CancellationToken.None);

        var response = result.Result.ShouldBeOfType<ObjectResult>();
        response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        response.Value.ShouldBeOfType<HealthResponse>().Status.ShouldBe("Unhealthy");
    }
}
