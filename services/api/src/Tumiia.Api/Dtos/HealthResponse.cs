namespace Tumiia.Api.Dtos;

/// <summary>The result of the health check.</summary>
/// <param name="Status">"Healthy" when every check passed, otherwise "Unhealthy" or "Degraded".</param>
/// <param name="Checks">The result of each individual check.</param>
public sealed record HealthResponse(string Status, IReadOnlyList<HealthCheckEntry> Checks);
