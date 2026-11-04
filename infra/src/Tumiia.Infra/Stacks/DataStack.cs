using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.S3;
using Constructs;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// The things that hold data: the PostgreSQL database and the documents bucket. Both are encrypted, private,
/// and protected from accidental deletion.
/// </summary>
public sealed class DataStack : Stack
{
    /// <summary>PostgreSQL 17 on RDS. Its generated credentials are in <see cref="DatabaseInstance.Secret"/>.</summary>
    public DatabaseInstance Database { get; }

    /// <summary>Holds every uploaded original under <c>originals/</c>. Versioned, so nothing is ever silently replaced.</summary>
    public Bucket DocumentsBucket { get; }

    /// <summary>Creates the database and the documents bucket.</summary>
    public DataStack(Construct scope, string id, IStackProps props, InfraSettings settings, ResourceNames names, IVpc vpc)
        : base(scope, id, props)
    {
        Database = CreateDatabase(names, vpc);
        DocumentsBucket = CreateDocumentsBucket(settings, names);
    }

    /// <summary>
    /// db.t4g.micro, 20 GB gp3, single AZ for now. Not reachable from the internet; 7 days of automatic backups.
    /// Only the major version is pinned, so RDS uses the newest 17.x it offers in Cape Town.
    /// </summary>
    private DatabaseInstance CreateDatabase(ResourceNames names, IVpc vpc)
    {
        return new DatabaseInstance(this, "Database", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps { Version = PostgresEngineVersion.Of("17", "17") }),
            InstanceType = InstanceType.Of(InstanceClass.BURSTABLE4_GRAVITON, InstanceSize.MICRO),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            DatabaseName = "tumiia",
            Credentials = Credentials.FromGeneratedSecret("tumiia_admin", new CredentialsBaseOptions { SecretName = names.DatabaseSecretName }),
            AllocatedStorage = 20,
            StorageType = StorageType.GP3,
            StorageEncrypted = true,
            PubliclyAccessible = false,
            MultiAz = false,
            BackupRetention = Duration.Days(7),
            DeletionProtection = true,
            AutoMinorVersionUpgrade = true,
            RemovalPolicy = RemovalPolicy.SNAPSHOT,
        });
    }

    /// <summary>
    /// Private, versioned, encrypted with S3-managed keys, HTTPS only. Browsers upload and download with
    /// pre-signed URLs, so CORS allows PUT and GET from the app's own address only.
    /// </summary>
    private Bucket CreateDocumentsBucket(InfraSettings settings, ResourceNames names)
    {
        return new Bucket(this, "DocumentsBucket", new BucketProps
        {
            BucketName = names.DocumentsBucketName,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Versioned = true,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            Cors =
            [
                new CorsRule
                {
                    AllowedMethods = [HttpMethods.PUT, HttpMethods.GET],
                    AllowedOrigins = [$"https://{settings.DomainName}"],
                    AllowedHeaders = ["*"],
                    ExposedHeaders = ["ETag"],
                    MaxAge = 3000,
                },
            ],
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "IntelligentTieringAfter30Days",
                    Transitions = [new Transition { StorageClass = StorageClass.INTELLIGENT_TIERING, TransitionAfter = Duration.Days(30) }],
                },
            ],
        });
    }
}
