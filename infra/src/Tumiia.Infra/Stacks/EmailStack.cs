using Amazon.CDK;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.SES;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// Lets the API send email (password resets) through SES in af-south-1. With a domain, it sends from the
/// domain and the DKIM records go into Route 53 automatically. Without one, it sends from a single address,
/// which SES emails a link to click first. New accounts start in the SES sandbox: see docs/deploy-setup.md.
/// </summary>
public sealed class EmailStack : Stack
{
    /// <summary>Creates the SES identity: the domain (with its DKIM records) or the single sender address.</summary>
    public EmailStack(Construct scope, string id, IStackProps props, InfraSettings settings)
        : base(scope, id, props)
    {
        if (settings.HasDomain)
        {
            CreateDomainIdentity(settings);
        }
        else
        {
            _ = new EmailIdentity(this, "SenderIdentity", new EmailIdentityProps { Identity = Identity.Email(settings.SenderEmail!) });
        }
    }

    private void CreateDomainIdentity(InfraSettings settings)
    {
        var zone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "Zone", new PublicHostedZoneAttributes
        {
            HostedZoneId = settings.HostedZoneId!,
            ZoneName = settings.HostedZoneName!,
        });

        _ = new EmailIdentity(this, "DomainIdentity", new EmailIdentityProps
        {
            Identity = Identity.PublicHostedZone(zone),
        });
    }
}
