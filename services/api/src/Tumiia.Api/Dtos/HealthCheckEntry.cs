namespace Tumiia.Api.Dtos;

/// <summary>The result of one health check, such as the database connection.</summary>
/// <param name="Name">What was checked, for example "database".</param>
/// <param name="Status">"Healthy", "Degraded" or "Unhealthy".</param>
public sealed record HealthCheckEntry(string Name, string Status);
