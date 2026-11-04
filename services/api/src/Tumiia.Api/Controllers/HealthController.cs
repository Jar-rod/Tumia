using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tumiia.Api.Dtos;

namespace Tumiia.Api.Controllers;

/// <summary>Tells load balancers and uptime checks whether the API is working.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
public sealed class HealthController(HealthCheckService healthChecks) : ControllerBase
{
    /// <summary>Checks that the API is running and can reach the database.</summary>
    /// <remarks>
    /// Anonymous, so the load balancer can call it. Returns 200 when every check passes and 503 when any fails.
    /// Only the name and result of each check are returned, never error details.
    /// </remarks>
    /// <param name="cancellationToken">Stops the checks if the caller goes away.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var response = ToResponse(report);

        if (report.Status == HealthStatus.Healthy)
        {
            return Ok(response);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    /// <summary>Turns the framework's health report into the small response the API returns.</summary>
    private static HealthResponse ToResponse(HealthReport report)
    {
        var checks = report.Entries
            .Select(entry => new HealthCheckEntry(entry.Key, entry.Value.Status.ToString()))
            .ToList();

        return new HealthResponse(report.Status.ToString(), checks);
    }
}
