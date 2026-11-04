using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// The VPC across two availability zones. Fargate tasks run in public subnets with public IPs, so there is
/// no NAT Gateway (saves about $35 a month); the database sits in isolated subnets with no internet route.
/// S3 traffic goes through a free gateway endpoint.
/// </summary>
public sealed class NetworkStack : Stack
{
    /// <summary>The VPC every other stack places its resources in.</summary>
    public Vpc Vpc { get; }

    /// <summary>
    /// The zones are stated here rather than looked up from the AWS account, so "cdk synth" and the tests work
    /// without AWS credentials. The VPC uses the first two.
    /// </summary>
    public override string[] AvailabilityZones => [$"{InfraSettings.Region}a", $"{InfraSettings.Region}b", $"{InfraSettings.Region}c"];

    /// <summary>Creates the VPC.</summary>
    public NetworkStack(Construct scope, string id, IStackProps props, InfraSettings settings)
        : base(scope, id, props)
    {
        Vpc = new Vpc(this, "Vpc", new VpcProps
        {
            VpcName = $"{settings.EnvironmentName}-tumiia",
            MaxAzs = 2,
            NatGateways = 0,
            SubnetConfiguration =
            [
                new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC, CidrMask = 24 },
                new SubnetConfiguration { Name = "database", SubnetType = SubnetType.PRIVATE_ISOLATED, CidrMask = 24 },
            ],
            GatewayEndpoints = new Dictionary<string, IGatewayVpcEndpointOptions>
            {
                ["S3"] = new GatewayVpcEndpointOptions { Service = GatewayVpcEndpointAwsService.S3 },
            },
        });
    }
}
