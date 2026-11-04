using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using CloudFrontFunction = Amazon.CDK.AWS.CloudFront.Function;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// Serves the React app and the API from one address through CloudFront: <c>/*</c> comes from a private S3
/// bucket, <c>/api/*</c> goes to the load balancer. One origin means no CORS and a strict refresh cookie.
/// </summary>
public sealed class WebStack : Stack
{
    /// <summary>
    /// Sends page addresses (no file extension, for example <c>/vault/123</c>) to <c>index.html</c> so the
    /// React router can handle them. Runs on the app behaviour only, so API 403s and 404s stay as they are.
    /// </summary>
    public const string SpaRoutingCode = """
        function handler(event) {
          var request = event.request;
          var lastPart = request.uri.split('/').pop();
          if (lastPart.indexOf('.') === -1) {
            request.uri = '/index.html';
          }
          return request;
        }
        """;

    /// <summary>Creates the web bucket, the CloudFront distribution and the DNS records for the app.</summary>
    public WebStack(
        Construct scope,
        string id,
        IStackProps props,
        InfraSettings settings,
        ResourceNames names,
        ICertificate certificate,
        ISecret originVerifySecret)
        : base(scope, id, props)
    {
        var webBucket = new Bucket(this, "WebBucket", new BucketProps
        {
            BucketName = names.WebBucketName,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
        });

        var securityHeaders = CreateSecurityHeaders(settings, names);
        var distribution = CreateDistribution(settings, webBucket, certificate, originVerifySecret, securityHeaders);
        AddDnsRecords(settings, distribution);

        _ = new CfnOutput(this, "WebBucketName", new CfnOutputProps { Value = webBucket.BucketName });
        _ = new CfnOutput(this, "DistributionId", new CfnOutputProps { Value = distribution.DistributionId });
        _ = new CfnOutput(this, "DistributionDomainName", new CfnOutputProps { Value = distribution.DistributionDomainName });
    }

    /// <summary>
    /// Browser security headers on every response. The content security policy only lets the page talk to
    /// itself and to the documents bucket (for pre-signed uploads, downloads and previews).
    /// </summary>
    private ResponseHeadersPolicy CreateSecurityHeaders(InfraSettings settings, ResourceNames names)
    {
        var documentsBucket = $"https://{names.DocumentsBucketName}.s3.{InfraSettings.Region}.amazonaws.com";
        var contentSecurityPolicy = string.Join("; ",
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self'",
            "font-src 'self'",
            $"img-src 'self' data: blob: {documentsBucket}",
            $"connect-src 'self' {documentsBucket}",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");

        return new ResponseHeadersPolicy(this, "SecurityHeaders", new ResponseHeadersPolicyProps
        {
            ResponseHeadersPolicyName = $"{settings.EnvironmentName}-tumiia-security-headers",
            SecurityHeadersBehavior = new ResponseSecurityHeadersBehavior
            {
                StrictTransportSecurity = new ResponseHeadersStrictTransportSecurity
                {
                    AccessControlMaxAge = Duration.Days(730),
                    IncludeSubdomains = true,
                    Override = true,
                },
                ContentTypeOptions = new ResponseHeadersContentTypeOptions { Override = true },
                ReferrerPolicy = new ResponseHeadersReferrerPolicy { ReferrerPolicy = HeadersReferrerPolicy.STRICT_ORIGIN_WHEN_CROSS_ORIGIN, Override = true },
                FrameOptions = new ResponseHeadersFrameOptions { FrameOption = HeadersFrameOption.DENY, Override = true },
                ContentSecurityPolicy = new ResponseHeadersContentSecurityPolicy { ContentSecurityPolicy = contentSecurityPolicy, Override = true },
            },
        });
    }

    /// <summary>
    /// <c>/*</c> serves the app from S3 (cached), sending page addresses to <c>index.html</c>.
    /// <c>/api/*</c> goes to the load balancer (see <see cref="ApiBehaviour"/>).
    /// </summary>
    private Distribution CreateDistribution(
        InfraSettings settings,
        IBucket webBucket,
        ICertificate certificate,
        ISecret originVerifySecret,
        IResponseHeadersPolicy securityHeaders)
    {
        var spaRouting = new CloudFrontFunction(this, "SpaRouting", new FunctionProps
        {
            Code = FunctionCode.FromInline(SpaRoutingCode),
            Runtime = FunctionRuntime.JS_2_0,
            Comment = "Send app page addresses to index.html",
        });

        return new Distribution(this, "Distribution", new DistributionProps
        {
            Comment = $"TUMIIA {settings.EnvironmentName}",
            DomainNames = [settings.DomainName],
            Certificate = certificate,
            DefaultRootObject = "index.html",
            MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021,
            HttpVersion = HttpVersion.HTTP2_AND_3,
            DefaultBehavior = new BehaviorOptions
            {
                Origin = S3BucketOrigin.WithOriginAccessControl(webBucket),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                CachePolicy = CachePolicy.CACHING_OPTIMIZED,
                ResponseHeadersPolicy = securityHeaders,
                FunctionAssociations = [new FunctionAssociation { Function = spaRouting, EventType = FunctionEventType.VIEWER_REQUEST }],
            },
            AdditionalBehaviors = new Dictionary<string, IBehaviorOptions>
            {
                ["/api/*"] = ApiBehaviour(settings, originVerifySecret, securityHeaders),
            },
        });
    }

    /// <summary>
    /// <c>/api/*</c>: never cached, every method allowed, all viewer headers, cookies and query strings passed on
    /// (including <c>CloudFront-Viewer-Address</c>), over HTTPS to the load balancer with the secret header.
    /// </summary>
    private static BehaviorOptions ApiBehaviour(InfraSettings settings, ISecret originVerifySecret, IResponseHeadersPolicy securityHeaders)
    {
        var apiOrigin = new HttpOrigin(ResourceNames.OriginHostName(settings.DomainName), new HttpOriginProps
        {
            ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
            CustomHeaders = new Dictionary<string, string>
            {
                [ResourceNames.OriginVerifyHeader] = originVerifySecret.SecretValue.UnsafeUnwrap(),
            },
        });

        return new BehaviorOptions
        {
            Origin = apiOrigin,
            ViewerProtocolPolicy = ViewerProtocolPolicy.HTTPS_ONLY,
            AllowedMethods = AllowedMethods.ALLOW_ALL,
            CachePolicy = CachePolicy.CACHING_DISABLED,
            OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_AND_CLOUDFRONT_2022,
            ResponseHeadersPolicy = securityHeaders,
        };
    }

    /// <summary>Points the app's address (IPv4 and IPv6) at CloudFront.</summary>
    private void AddDnsRecords(InfraSettings settings, IDistribution distribution)
    {
        var zone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "Zone", new PublicHostedZoneAttributes
        {
            HostedZoneId = settings.HostedZoneId,
            ZoneName = settings.HostedZoneName,
        });
        var target = RecordTarget.FromAlias(new CloudFrontTarget(distribution));

        _ = new ARecord(this, "AppRecord", new ARecordProps { Zone = zone, RecordName = settings.DomainName, Target = target });
        _ = new AaaaRecord(this, "AppRecordIpv6", new AaaaRecordProps { Zone = zone, RecordName = settings.DomainName, Target = target });
    }
}
