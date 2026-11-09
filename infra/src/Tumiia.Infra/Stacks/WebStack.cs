using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SSM;
using Constructs;
using CloudFrontFunction = Amazon.CDK.AWS.CloudFront.Function;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// Serves the React app and the API from one address through CloudFront: <c>/*</c> comes from a private S3
/// bucket, <c>/api/*</c> goes to the internal load balancer through a VPC origin. One origin means no CORS and a
/// strict refresh cookie. With a domain, CloudFront answers on it; without one, on its own <c>*.cloudfront.net</c>
/// address.
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

    /// <summary>
    /// Creates the web bucket, the CloudFront distribution, the DNS records (with a domain) and the parameter
    /// that tells the API its own address. <paramref name="certificate"/> is null when there is no domain.
    /// </summary>
    public WebStack(
        Construct scope,
        string id,
        IStackProps props,
        InfraSettings settings,
        ResourceNames names,
        ICertificate? certificate,
        IApplicationLoadBalancer loadBalancer,
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
        var distribution = CreateDistribution(settings, webBucket, certificate, ApiOrigin(loadBalancer, originVerifySecret), securityHeaders);
        if (settings.HasDomain)
        {
            AddDnsRecords(settings, distribution);
        }

        PublishAppBaseUrl(settings, names, distribution);

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
        ICertificate? certificate,
        IOrigin apiOrigin,
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
            DomainNames = settings.HasDomain ? [settings.DomainName!] : null,
            Certificate = certificate,
            DefaultRootObject = "index.html",
            // CloudFront's default certificate has a fixed TLS policy, so the minimum only applies with a domain.
            MinimumProtocolVersion = settings.HasDomain ? SecurityPolicyProtocol.TLS_V1_2_2021 : null,
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
                ["/api/*"] = ApiBehaviour(apiOrigin, securityHeaders),
            },
        });
    }

    /// <summary>
    /// The internal load balancer, reached through a CloudFront VPC origin: CloudFront connects to it inside the
    /// VPC, so it needs no public address or certificate. The secret header lets the API reject anything else.
    /// </summary>
    private static IOrigin ApiOrigin(IApplicationLoadBalancer loadBalancer, ISecret originVerifySecret)
    {
        return VpcOrigin_.WithApplicationLoadBalancer(loadBalancer, new VpcOriginWithEndpointProps
        {
            ProtocolPolicy = OriginProtocolPolicy.HTTP_ONLY,
            HttpPort = 80,
            CustomHeaders = new Dictionary<string, string>
            {
                [ResourceNames.OriginVerifyHeader] = originVerifySecret.SecretValue.UnsafeUnwrap(),
            },
        });
    }

    /// <summary>
    /// <c>/api/*</c>: never cached, every method allowed, all viewer headers, cookies and query strings passed on
    /// (including <c>CloudFront-Viewer-Address</c>), to the load balancer with the secret header.
    /// </summary>
    private static BehaviorOptions ApiBehaviour(IOrigin apiOrigin, IResponseHeadersPolicy securityHeaders)
    {
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

    /// <summary>
    /// Writes the app's public address (the domain, or the CloudFront address) to SSM, where the API reads it for
    /// the links in emails.
    /// </summary>
    private void PublishAppBaseUrl(InfraSettings settings, ResourceNames names, IDistribution distribution)
    {
        var host = settings.HasDomain ? settings.DomainName! : distribution.DistributionDomainName;

        _ = new StringParameter(this, "AppBaseUrl", new StringParameterProps
        {
            ParameterName = names.AppBaseUrlParameterName,
            Description = "The app's public address, used by the API for links in emails.",
            StringValue = $"https://{host}",
        });
    }

    /// <summary>Points the app's address (IPv4 and IPv6) at CloudFront.</summary>
    private void AddDnsRecords(InfraSettings settings, IDistribution distribution)
    {
        var zone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "Zone", new PublicHostedZoneAttributes
        {
            HostedZoneId = settings.HostedZoneId!,
            ZoneName = settings.HostedZoneName!,
        });
        var target = RecordTarget.FromAlias(new CloudFrontTarget(distribution));

        _ = new ARecord(this, "AppRecord", new ARecordProps { Zone = zone, RecordName = settings.DomainName, Target = target });
        _ = new AaaaRecord(this, "AppRecordIpv6", new AaaaRecordProps { Zone = zone, RecordName = settings.DomainName, Target = target });
    }
}
