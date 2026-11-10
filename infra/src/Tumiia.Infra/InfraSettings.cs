using Constructs;

namespace Tumiia.Infra;

/// <summary>
/// Everything the stacks need to know about this deployment, read from CDK context
/// (<c>-c key=value</c> on the command line, or the "context" block in cdk.json).
/// </summary>
public sealed record InfraSettings
{
    /// <summary>Every TUMIIA resource lives in Cape Town.</summary>
    public const string Region = "af-south-1";

    /// <summary>CloudFront only accepts certificates from this region.</summary>
    public const string CloudFrontCertificateRegion = "us-east-1";

    /// <summary>Environment name, used as a prefix on every resource name ("prod", later "staging").</summary>
    public required string EnvironmentName { get; init; }

    /// <summary>The 12-digit AWS account id.</summary>
    public required string Account { get; init; }

    /// <summary>
    /// The address users open, for example <c>app.example.co.za</c>. Optional: without it, users open the
    /// CloudFront address (<c>d1234abcd.cloudfront.net</c>) and email comes from <see cref="SenderEmail"/>.
    /// </summary>
    public string? DomainName { get; init; }

    /// <summary>The Route 53 hosted zone that holds <see cref="DomainName"/>. Required with a domain.</summary>
    public string? HostedZoneId { get; init; }

    /// <summary>The hosted zone's own name, for example <c>example.co.za</c>. Defaults to <see cref="DomainName"/>.</summary>
    public string? HostedZoneName { get; init; }

    /// <summary>
    /// The address password reset emails come from when there is no domain. SES emails it a link to click
    /// before it can send. Defaults to <see cref="AlertEmail"/>. Ignored with a domain (<c>no-reply@domain</c>).
    /// </summary>
    public string? SenderEmail { get; init; }

    /// <summary>
    /// The GitHub repository as it appears in the OIDC token's subject: <c>owner/name</c>, or the immutable form
    /// <c>owner@ownerId/name@repoId</c> for repositories created after 15 July 2026.
    /// </summary>
    public required string GitHubRepo { get; init; }

    /// <summary>Where alarm emails go.</summary>
    public required string AlertEmail { get; init; }

    /// <summary>Id of the AWS-managed CloudFront origin-facing prefix list in af-south-1 (looked up once by hand).</summary>
    public required string CloudFrontPrefixListId { get; init; }

    /// <summary>Comma-separated emails that get the Admin role when they register or sign in. Optional.</summary>
    public string? AdminEmails { get; init; }

    /// <summary>The image tag the API service runs. Null before the first backend deploy, which keeps the service at zero tasks.</summary>
    public string? ImageTag { get; init; }

    /// <summary>
    /// When true, the API serves Swagger UI at <c>/swagger</c> and the OpenAPI documents at <c>/openapi/</c>, through
    /// CloudFront. Off by default: it publishes the API's description to anyone with the address.
    /// </summary>
    public bool EnableSwagger { get; init; }

    /// <summary>The Bedrock model that reads documents unless <c>bedrockModelId</c> says otherwise.</summary>
    public const string DefaultBedrockModelId = "anthropic.claude-haiku-5-5";

    /// <summary>
    /// When true, the API reads and classifies documents with Claude on Amazon Bedrock, and its task role may call it.
    /// Off when not set. From af-south-1, Bedrock serves Claude only through its global endpoint, so documents are
    /// processed outside South Africa.
    /// </summary>
    public bool EnableBedrock { get; init; }

    /// <summary>The Bedrock model id, such as <c>anthropic.claude-haiku-5-5</c> (the default).</summary>
    public string BedrockModelId { get; init; } = DefaultBedrockModelId;

    /// <summary>Reads the settings from CDK context and stops with a clear message when a required one is missing.</summary>
    public static InfraSettings FromContext(Node node)
    {
        var domainName = Optional(node, "domainName");
        var alertEmail = Required(node, "alertEmail");
        var settings = new InfraSettings
        {
            EnvironmentName = Optional(node, "env") ?? "prod",
            Account = Required(node, "account"),
            DomainName = domainName,
            HostedZoneId = Optional(node, "hostedZoneId"),
            HostedZoneName = Optional(node, "hostedZoneName") ?? domainName,
            SenderEmail = Optional(node, "senderEmail") ?? alertEmail,
            GitHubRepo = Required(node, "githubRepo"),
            AlertEmail = alertEmail,
            CloudFrontPrefixListId = Required(node, "cloudFrontPrefixListId"),
            AdminEmails = Optional(node, "adminEmails"),
            ImageTag = Optional(node, "imageTag"),
            EnableSwagger = IsTrue(node, "enableSwagger"),
            EnableBedrock = IsTrue(node, "enableBedrock"),
            BedrockModelId = Optional(node, "bedrockModelId") ?? DefaultBedrockModelId,
        };

        settings.CheckDomainSettings();
        return settings;
    }

    /// <summary>True when the app has its own domain in Route 53; false when it uses the CloudFront address.</summary>
    public bool HasDomain => DomainName is not null;

    /// <summary>The address password reset emails come from: <c>no-reply@domain</c>, or <see cref="SenderEmail"/>.</summary>
    public string EmailFromAddress => HasDomain ? $"no-reply@{DomainName}" : SenderEmail!;

    /// <summary>The SES identity the API sends as: the domain, or the single sender address.</summary>
    public string EmailIdentity => DomainName ?? SenderEmail!;

    private void CheckDomainSettings()
    {
        if (HasDomain && HostedZoneId is null)
        {
            throw new ArgumentException("domainName also needs -c hostedZoneId=... (the Route 53 zone that holds it).");
        }
    }

    private static string Required(Node node, string key)
    {
        return Optional(node, key)
            ?? throw new ArgumentException($"Missing CDK context '{key}'. Pass it with: -c {key}=<value>. See docs/deploy-setup.md.");
    }

    private static bool IsTrue(Node node, string key)
    {
        return string.Equals(Optional(node, key), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Optional(Node node, string key)
    {
        var value = node.TryGetContext(key)?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
