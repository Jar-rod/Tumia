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
                ["CacheBehaviors"] = Match.ArrayWith(new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["PathPattern"] = "/api/*",
                        // AWS-managed policies: CachingDisabled, and AllViewerAndCloudFrontHeaders-2022-06.
                        ["CachePolicyId"] = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad",
                        ["OriginRequestPolicyId"] = "33f36d7e-f396-46d9-90e0-52428a34d9dc",
                    }),
                }),
            }),
        }));
    }

    [Fact]
    public void SwaggerAndOpenApiPaths_GoToTheApiLikeApiPaths()
    {
        var behaviours = DistributionConfig()["CacheBehaviors"]!.AsArray();
        var api = behaviours.Single(behaviour => behaviour!["PathPattern"]!.GetValue<string>() == "/api/*")!;

        var swaggerPaths = behaviours.Where(behaviour => behaviour!["PathPattern"]!.GetValue<string>() is "/swagger*" or "/openapi/*").ToList();

        swaggerPaths.Count.ShouldBe(2);
        swaggerPaths.ShouldAllBe(behaviour => behaviour!["TargetOriginId"]!.ToJsonString() == api["TargetOriginId"]!.ToJsonString());
        swaggerPaths.ShouldAllBe(behaviour => behaviour!["CachePolicyId"]!.GetValue<string>() == api["CachePolicyId"]!.GetValue<string>());
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
        config["CacheBehaviors"]!.AsArray().ShouldAllBe(behaviour => behaviour!["FunctionAssociations"] == null);
    }

    [Fact]
    public void ApiOrigin_IsTheLoadBalancerThroughAVpcOriginOverHttp()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Web);

        template.HasResourceProperties("AWS::CloudFront::VpcOrigin", Match.ObjectLike(new Dictionary<string, object>
        {
            ["VpcOriginEndpointConfig"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["OriginProtocolPolicy"] = "http-only",
                ["HTTPPort"] = 80,
            }),
        }));
    }

    [Fact]
    public void ApiOrigin_SendsTheOriginSecretFromSecretsManager()
    {
        var config = DistributionConfig();
        var apiOrigin = config["Origins"]!.AsArray().Single(origin => origin!["VpcOriginConfig"] is not null)!;

        var header = apiOrigin["OriginCustomHeaders"]![0]!;

        header["HeaderName"]!.GetValue<string>().ShouldBe("X-Origin-Verify");
        header["HeaderValue"]!.ToJsonString().ShouldContain("{{resolve:secretsmanager:");
    }

    [Fact]
    public void Distribution_WithoutADomain_UsesTheCloudFrontAddressAndNoCertificate()
    {
        var config = DistributionConfig();

        config["Aliases"].ShouldBeNull();
        stacks.BeforeFirstDeploy.WebCertificate.ShouldBeNull();
        Template.FromStack(stacks.BeforeFirstDeploy.Web).ResourceCountIs("AWS::Route53::RecordSet", 0);
    }

    [Fact]
    public void Distribution_WithADomain_AnswersOnItAndPointsItsDnsAtCloudFront()
    {
        var template = Template.FromStack(stacks.WithDomain.Web);

        template.HasResourceProperties("AWS::CloudFront::Distribution", Match.ObjectLike(new Dictionary<string, object>
        {
            ["DistributionConfig"] = Match.ObjectLike(new Dictionary<string, object> { ["Aliases"] = new[] { SynthesizedStacks.DomainName } }),
        }));
        template.ResourceCountIs("AWS::Route53::RecordSet", 2);
        stacks.WithDomain.WebCertificate.ShouldNotBeNull();
    }

    [Fact]
    public void AppBaseUrl_WithoutADomain_IsTheCloudFrontAddress()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.Web);

        var parameter = SynthesizedStacks.Resources(template, "AWS::SSM::Parameter").Single()["Properties"]!;

        parameter["Name"]!.GetValue<string>().ShouldBe("/prod/tumiia/app-base-url");
        parameter["Value"]!.ToJsonString().ShouldContain("DomainName");
    }

    [Fact]
    public void AppBaseUrl_WithADomain_IsTheDomain()
    {
        var template = Template.FromStack(stacks.WithDomain.Web);

        template.HasResourceProperties("AWS::SSM::Parameter", Match.ObjectLike(new Dictionary<string, object>
        {
            ["Value"] = $"https://{SynthesizedStacks.DomainName}",
        }));
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
