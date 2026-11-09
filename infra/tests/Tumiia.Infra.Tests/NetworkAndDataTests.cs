using System.Text.Json.Nodes;
using Amazon.CDK.Assertions;

namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class NetworkAndDataTests(SynthesizedStacks stacks)
{
    [Fact]
    public void Network_HasNoNatGateway()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Network);

        template.ResourceCountIs("AWS::EC2::NatGateway", 0);
    }

    [Fact]
    public void Network_SendsS3TrafficThroughTheFreeGatewayEndpoint()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Network);

        template.ResourceCountIs("AWS::EC2::VPCEndpoint", 1);
    }

    [Fact]
    public void Database_IsNotPublicAndIsEncryptedAndProtected()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Data);

        template.HasResourceProperties("AWS::RDS::DBInstance", new Dictionary<string, object>
        {
            ["PubliclyAccessible"] = false,
            ["StorageEncrypted"] = true,
            ["DeletionProtection"] = true,
            ["BackupRetentionPeriod"] = 7,
        });
    }

    [Fact]
    public void Database_IsPostgres17OnTheSmallestGravitonInstance()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Data);

        template.HasResourceProperties("AWS::RDS::DBInstance", new Dictionary<string, object>
        {
            ["Engine"] = "postgres",
            ["EngineVersion"] = "17",
            ["DBInstanceClass"] = "db.t4g.micro",
            ["StorageType"] = "gp3",
            ["AllocatedStorage"] = "20",
        });
    }

    [Fact]
    public void EveryBucket_BlocksAllPublicAccess()
    {
        var buckets = SynthesizedStacks.Resources(SynthesizedStacks.Json(stacks.BeforeFirstDeploy.Data), "AWS::S3::Bucket")
            .Concat(SynthesizedStacks.Resources(SynthesizedStacks.Json(stacks.BeforeFirstDeploy.Web), "AWS::S3::Bucket"))
            .ToList();

        buckets.Count.ShouldBe(2);
        buckets.ShouldAllBe(bucket => BlocksAllPublicAccess(bucket));
    }

    [Fact]
    public void Network_PutsTheDatabaseAndTheLoadBalancerInSeparateIsolatedSubnets()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.Network).ToJsonString();

        template.ShouldContain("\"Value\":\"database\"");
        template.ShouldContain("\"Value\":\"load-balancer\"");
    }

    [Fact]
    public void DocumentsBucket_WithoutADomain_AcceptsBrowserUploadsFromCloudFrontAddresses()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Data);

        template.HasResourceProperties("AWS::S3::Bucket", Match.ObjectLike(new Dictionary<string, object>
        {
            ["CorsConfiguration"] = new Dictionary<string, object>
            {
                ["CorsRules"] = new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object> { ["AllowedOrigins"] = new[] { "https://*.cloudfront.net" } }),
                },
            },
        }));
    }

    [Fact]
    public void DocumentsBucket_IsVersionedAndOnlyAcceptsBrowserUploadsFromTheApp()
    {
        var template = Template.FromStack(stacks.WithDomain.Data);

        template.HasResourceProperties("AWS::S3::Bucket", Match.ObjectLike(new Dictionary<string, object>
        {
            ["VersioningConfiguration"] = new Dictionary<string, object> { ["Status"] = "Enabled" },
            ["CorsConfiguration"] = new Dictionary<string, object>
            {
                ["CorsRules"] = new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["AllowedMethods"] = new[] { "PUT", "GET" },
                        ["AllowedOrigins"] = new[] { $"https://{SynthesizedStacks.DomainName}" },
                    }),
                },
            },
        }));
    }

    private static bool BlocksAllPublicAccess(JsonNode bucket)
    {
        var block = bucket["Properties"]?["PublicAccessBlockConfiguration"];
        return block is not null
            && block["BlockPublicAcls"]!.GetValue<bool>()
            && block["BlockPublicPolicy"]!.GetValue<bool>()
            && block["IgnorePublicAcls"]!.GetValue<bool>()
            && block["RestrictPublicBuckets"]!.GetValue<bool>();
    }
}
