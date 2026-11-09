using Amazon.CDK;
using Tumiia.Infra.Stacks;

namespace Tumiia.Infra;

/// <summary>Creates every TUMIIA stack in the right order. Used by Program.cs and by the tests.</summary>
public sealed class TumiiaStacks
{
    public required GitHubOidcStack GitHubOidc { get; init; }

    public required NetworkStack Network { get; init; }

    public required DataStack Data { get; init; }

    public required MessagingStack Messaging { get; init; }

    public required AppStack App { get; init; }

    /// <summary>Null when the app has no domain of its own (it then uses the CloudFront address).</summary>
    public required WebCertificateStack? WebCertificate { get; init; }

    public required WebStack Web { get; init; }

    public required EmailStack Email { get; init; }

    /// <summary>
    /// Adds all stacks to the CDK app. Everything is in af-south-1 except the CloudFront certificate, which is
    /// only made when the app has its own domain.
    /// </summary>
    public static TumiiaStacks Create(App app, InfraSettings settings)
    {
        var names = new ResourceNames(settings.EnvironmentName, settings.Account);
        var capeTown = PropsFor(settings, InfraSettings.Region);
        var usEast1 = PropsFor(settings, InfraSettings.CloudFrontCertificateRegion);

        var gitHubOidc = new GitHubOidcStack(app, names.Stack("github-oidc"), capeTown("GitHub Actions deploy role"), settings, names);
        var network = new NetworkStack(app, names.Stack("network"), capeTown("VPC"), settings);
        var data = new DataStack(app, names.Stack("data"), capeTown("Database and documents bucket"), settings, names, network.Vpc);
        var messaging = new MessagingStack(app, names.Stack("messaging"), capeTown("Analysis queue and alarms"), settings, names);
        var appStack = new AppStack(app, names.Stack("app"), capeTown("API on ECS Fargate behind a load balancer"), settings, names, network.Vpc, data, messaging);
        var webCertificate = settings.HasDomain
            ? new WebCertificateStack(app, names.Stack("web-certificate"), usEast1("CloudFront certificate"), settings)
            : null;
        var web = new WebStack(app, names.Stack("web"), capeTown("React app on S3 and CloudFront"), settings, names, webCertificate?.Certificate, appStack.LoadBalancer, appStack.OriginVerifySecret);
        var email = new EmailStack(app, names.Stack("email"), capeTown("SES email identity"), settings);

        return new TumiiaStacks
        {
            GitHubOidc = gitHubOidc,
            Network = network,
            Data = data,
            Messaging = messaging,
            App = appStack,
            WebCertificate = webCertificate,
            Web = web,
            Email = email,
        };
    }

    // Cross-region references let the af-south-1 web stack use the us-east-1 certificate.
    private static Func<string, StackProps> PropsFor(InfraSettings settings, string region)
    {
        return description => new StackProps
        {
            Env = new Amazon.CDK.Environment { Account = settings.Account, Region = region },
            Description = $"TUMIIA {settings.EnvironmentName}: {description}",
            CrossRegionReferences = true,
            Tags = new Dictionary<string, string> { ["project"] = "tumiia", ["env"] = settings.EnvironmentName },
        };
    }
}
