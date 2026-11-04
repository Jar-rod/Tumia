using Amazon.CDK;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.SES;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// Lets the API send email (password resets) from the app's domain through SES in af-south-1. The DKIM
/// records go into Route 53 automatically. New accounts start in the SES sandbox: see docs/deploy-setup.md.
/// </summary>
public sealed class EmailStack : Stack
{
    /// <summary>Creates the SES domain identity and its DKIM records.</summary>
    public EmailStack(Construct scope, string id, IStackProps props, InfraSettings settings)
        : base(scope, id, props)
    {
        var zone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "Zone", new PublicHostedZoneAttributes
        {
            HostedZoneId = settings.HostedZoneId,
            ZoneName = settings.HostedZoneName,
        });

        _ = new EmailIdentity(this, "DomainIdentity", new EmailIdentityProps
        {
            Identity = Identity.PublicHostedZone(zone),
        });
    }
}
