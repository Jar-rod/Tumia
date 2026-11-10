using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using Tumiia.Domain.Email;
using Tumiia.Infrastructure;
using Tumiia.Infrastructure.Aws;

namespace Tumiia.IntegrationTests;

/// <summary>
/// Starts the real API in memory, backed by a real PostgreSQL 17 database and a LocalStack (S3 and SQS) container,
/// with S3 checking pre-signed URL signatures like real S3 does.
/// The app runs as Development, so it applies the migrations to the container on start-up.
/// Rate limits are raised so tests do not trip each other; the rate-limit test puts them back. The analysis worker
/// is off; tests call the analysis themselves.
/// </summary>
public sealed class TumiiaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Gets the Admin role through <c>Admin:Emails</c>.</summary>
    public const string AdminEmail = "admin@tumiia.test";

    /// <summary>The bucket for original files, created when the containers start.</summary>
    public const string DocumentsBucket = "test-tumiia-documents";

    private const string QueueName = "test-document-analysis";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    private readonly LocalStackContainer _localStack = new LocalStackBuilder("localstack/localstack:4.9")
        .WithEnvironment("SERVICES", "s3,sqs")
        .WithEnvironment("S3_SKIP_SIGNATURE_VALIDATION", "0")
        .Build();

    /// <summary>The analysis queue's URL in LocalStack.</summary>
    public string QueueUrl { get; private set; } = string.Empty;

    /// <summary>Settings that point AWS clients at the LocalStack container.</summary>
    public AwsSettings AwsSettings => new() { ServiceUrl = _localStack.GetConnectionString() };

    /// <summary>Emails the app "sent" (nothing really goes out).</summary>
    public CapturedEmails Emails { get; } = new();

    /// <summary>Starts the containers, then creates the bucket and the queue, before any test runs.</summary>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _localStack.StartAsync());

        using var s3 = AwsClients.CreateS3(AwsSettings);
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = DocumentsBucket, BucketRegion = S3Region.AFSouth1 });
        using var sqs = AwsClients.CreateSqs(AwsSettings);
        QueueUrl = (await sqs.CreateQueueAsync(QueueName)).QueueUrl;
    }

    /// <summary>A client on https://localhost (so the Secure refresh cookie works). Cookies are handled by the tests.</summary>
    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
    }

    /// <summary>Points the app at the test database, raises the rate limits and captures emails.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _database.GetConnectionString());
        builder.UseSetting("Aws:ServiceUrl", _localStack.GetConnectionString());
        builder.UseSetting("Storage:DocumentsBucket", DocumentsBucket);
        builder.UseSetting("Queues:DocumentAnalysisUrl", QueueUrl);
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Default:PermitLimit", "100000");
        builder.UseSetting("Admin:Emails", AdminEmail);
        // The tests read the queue themselves and run the analysis directly, so the background worker stays off.
        builder.UseSetting("Analysis:WorkerEnabled", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Emails));
    }

    /// <summary>Stops the app, then removes the database container.</summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        await _localStack.DisposeAsync();
    }
}
