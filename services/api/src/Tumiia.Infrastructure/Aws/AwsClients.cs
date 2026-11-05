using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.SQS;

namespace Tumiia.Infrastructure.Aws;

/// <summary>
/// Creates the AWS clients the same way everywhere: for real AWS (region only; credentials from the task role), or
/// for LocalStack (its address, and LocalStack's dummy "test" credentials).
/// </summary>
public static class AwsClients
{
    private static readonly BasicAWSCredentials LocalStackCredentials = new("test", "test");

    /// <summary>The S3 client. LocalStack needs path-style addresses (<c>host/bucket/key</c>).</summary>
    public static IAmazonS3 CreateS3(AwsSettings settings)
    {
        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) };
        if (!settings.UsesLocalStack)
        {
            return new AmazonS3Client(config);
        }

        config.ServiceURL = settings.ServiceUrl;
        config.ForcePathStyle = true;
        config.AuthenticationRegion = settings.Region;
        return new AmazonS3Client(LocalStackCredentials, config);
    }

    /// <summary>The SQS client.</summary>
    public static IAmazonSQS CreateSqs(AwsSettings settings)
    {
        var config = new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) };
        if (!settings.UsesLocalStack)
        {
            return new AmazonSQSClient(config);
        }

        config.ServiceURL = settings.ServiceUrl;
        config.AuthenticationRegion = settings.Region;
        return new AmazonSQSClient(LocalStackCredentials, config);
    }

    /// <summary>The SES v2 client. SES is only used in AWS, so there is no LocalStack variant.</summary>
    public static IAmazonSimpleEmailServiceV2 CreateSes(AwsSettings settings)
    {
        return new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(settings.Region));
    }
}
