using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Route53;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// The HTTPS certificate for the app's address. It lives in us-east-1 because CloudFront only accepts
/// certificates from there. It holds no data. Only created when the app has its own domain.
/// </summary>
public sealed class WebCertificateStack : Stack
{
    /// <summary>Certificate for the app's domain, validated through a DNS record in Route 53.</summary>
    public Certificate Certificate { get; }

    /// <summary>Creates the certificate in us-east-1.</summary>
    public WebCertificateStack(Construct scope, string id, IStackProps props, InfraSettings settings)
        : base(scope, id, props)
    {
        var zone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "Zone", new PublicHostedZoneAttributes
        {
            HostedZoneId = settings.HostedZoneId!,
            ZoneName = settings.HostedZoneName!,
        });

        Certificate = new Certificate(this, "Certificate", new CertificateProps
        {
            DomainName = settings.DomainName!,
            Validation = CertificateValidation.FromDns(zone),
        });
    }
}
