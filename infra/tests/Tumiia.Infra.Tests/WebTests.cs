using System.Text.Json.Nodes;
using Amazon.CDK.Assertions;

namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class WebTests(SynthesizedStacks stacks)
{
    private JsonNode DistributionConfig()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.Web);
        return SynthesizedStacks.Resources(template, "AWS::CloudFront::Distribution").Single()["Properties"]!["DistributionConfig"]!;
    }

    [Fact]
    public void ApiPath_IsNeverCachedAndPassesTheViewersAddress()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Web);

        template.HasResourceProperties("AWS::CloudFront::Distribution", Match.ObjectLike(new Dictionary<string, object>
        {
            ["DistributionConfig"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["CacheBehaviors"] = new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["PathPattern"] = "/api/*",
                        // AWS-managed policies: CachingDisabled, and AllViewerAndCloudFrontHeaders-2022-06.
                        ["CachePolicyId"] = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad",
                        ["OriginRequestPolicyId"] = "33f36d7e-f396-46d9-90e0-52428a34d9dc",
                    }),
                },
            }),
        }));
    }

    [Fact]
    public void Distribution_HasNoSiteWideErrorPages_SoApiErrorsReachTheCaller()
    {
        var config = DistributionConfig();

        config["CustomErrorResponses"].ShouldBeNull();
    }

    [Fact]
    public void SpaRouting_RunsOnTheAppBehaviourOnly()
    {
        var config = DistributionConfig();

        config["DefaultCacheBehavior"]!["FunctionAssociations"].ShouldNotBeNull();
        config["CacheBehaviors"]![0]!["FunctionAssociations"].ShouldBeNull();
    }

    [Fact]
    public void ApiOrigin_SendsTheOriginSecretFromSecretsManager()
    {
        var config = DistributionConfig();
        var apiOrigin = config["Origins"]!.AsArray().Single(origin => origin!["CustomOriginConfig"] is not null)!;

        var header = apiOrigin["OriginCustomHeaders"]![0]!;

        header["HeaderName"]!.GetValue<string>().ShouldBe("X-Origin-Verify");
        header["HeaderValue"]!.ToJsonString().ShouldContain("{{resolve:secretsmanager:");
    }

    [Fact]
    public void Responses_CarryTheSecurityHeaders()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Web);

        template.HasResourceProperties("AWS::CloudFront::ResponseHeadersPolicy", Match.ObjectLike(new Dictionary<string, object>
        {
            ["ResponseHeadersPolicyConfig"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["SecurityHeadersConfig"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["FrameOptions"] = new Dictionary<string, object> { ["FrameOption"] = "DENY", ["Override"] = true },
                    ["ContentTypeOptions"] = new Dictionary<string, object> { ["Override"] = true },
                    ["StrictTransportSecurity"] = Match.ObjectLike(new Dictionary<string, object> { ["IncludeSubdomains"] = true }),
                    ["ContentSecurityPolicy"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["ContentSecurityPolicy"] = Match.StringLikeRegexp("connect-src 'self' https://prod-tumiia-documents-123456789012\\.s3\\.af-south-1\\.amazonaws\\.com"),
                    }),
                }),
            }),
        }));
    }
}
