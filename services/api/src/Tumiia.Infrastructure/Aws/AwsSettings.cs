namespace Tumiia.Infrastructure.Aws;

/// <summary>
/// AWS settings, from the <c>Aws</c> section. In AWS only the region is set and credentials come from the ECS task
/// role. Locally, <see cref="ServiceUrl"/> points every client at LocalStack instead.
/// </summary>
public sealed class AwsSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Aws";

    /// <summary>The AWS region. Cape Town.</summary>
    public string Region { get; set; } = "af-south-1";

    /// <summary>LocalStack's address (for example <c>http://localhost:4566</c>), or null in AWS.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>True when talking to LocalStack rather than real AWS.</summary>
    public bool UsesLocalStack => !string.IsNullOrWhiteSpace(ServiceUrl);
}
