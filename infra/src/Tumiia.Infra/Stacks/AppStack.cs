using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SSM;
using Constructs;
using EcsSecret = Amazon.CDK.AWS.ECS.Secret;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// The API: its image repository, the ECS Fargate service that runs it (ARM64, 0.5 vCPU / 1 GB), and the internal
/// load balancer in front of it. The load balancer has no public address; only CloudFront reaches it, through a
/// VPC origin (see <see cref="WebStack"/>).
/// </summary>
public sealed class AppStack : Stack
{
    private const int ContainerPort = 8080;

    /// <summary>
    /// Shared secret CloudFront sends in the <c>X-Origin-Verify</c> header, so the API can reject requests that
    /// did not come through CloudFront. Generated here; nobody needs to type or see it.
    /// </summary>
    public ISecret OriginVerifySecret { get; }

    /// <summary>The internal load balancer CloudFront sends <c>/api/*</c> to.</summary>
    public ApplicationLoadBalancer LoadBalancer { get; }

    /// <summary>Creates the API's image repository, task definition, service and load balancer.</summary>
    public AppStack(
        Construct scope,
        string id,
        IStackProps props,
        InfraSettings settings,
        ResourceNames names,
        IVpc vpc,
        DataStack data,
        MessagingStack messaging)
        : base(scope, id, props)
    {
        var repository = CreateImageRepository(names);
        OriginVerifySecret = new Amazon.CDK.AWS.SecretsManager.Secret(this, "OriginVerifySecret", new SecretProps
        {
            SecretName = names.OriginVerifySecretName,
            Description = "CloudFront sends this in X-Origin-Verify; the API rejects requests without it.",
            GenerateSecretString = new SecretStringGenerator { PasswordLength = 48, ExcludePunctuation = true },
        });

        var taskRole = CreateTaskRole(settings, names, data, messaging);
        var taskDefinition = CreateTaskDefinition(settings, names, repository, taskRole, data, messaging);
        var service = CreateService(settings, names, vpc, taskDefinition);
        AllowServiceToReachDatabase(service, data);

        LoadBalancer = CreateLoadBalancer(settings, vpc, service);

        _ = new CfnOutput(this, "LoadBalancerDnsName", new CfnOutputProps { Value = LoadBalancer.LoadBalancerDnsName });
        _ = new CfnOutput(this, "ClusterName", new CfnOutputProps { Value = names.ClusterName });
        _ = new CfnOutput(this, "ServiceName", new CfnOutputProps { Value = names.ServiceName });
    }

    /// <summary>
    /// Lets the API tasks connect to PostgreSQL. The rule is added from this stack, to the database's security
    /// group, because adding it from the data stack would make the two stacks depend on each other.
    /// </summary>
    private void AllowServiceToReachDatabase(FargateService service, DataStack data)
    {
        var databaseSecurityGroup = SecurityGroup.FromSecurityGroupId(
            this,
            "DatabaseSecurityGroup",
            data.Database.Connections.SecurityGroups[0].SecurityGroupId);

        _ = new CfnSecurityGroupIngress(this, "DatabaseFromApi", new CfnSecurityGroupIngressProps
        {
            GroupId = databaseSecurityGroup.SecurityGroupId,
            SourceSecurityGroupId = service.Connections.SecurityGroups[0].SecurityGroupId,
            IpProtocol = "tcp",
            FromPort = 5432,
            ToPort = 5432,
            Description = "API tasks to PostgreSQL",
        });
    }

    /// <summary>Scans every pushed image; keeps the last 20 images and deletes untagged ones after 7 days.</summary>
    private Repository CreateImageRepository(ResourceNames names)
    {
        return new Repository(this, "ImageRepository", new RepositoryProps
        {
            RepositoryName = names.RepositoryName,
            ImageScanOnPush = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule { RulePriority = 1, Description = "Delete untagged images after 7 days", TagStatus = TagStatus.UNTAGGED, MaxImageAge = Duration.Days(7) },
                new LifecycleRule { RulePriority = 2, Description = "Keep the last 20 images", TagStatus = TagStatus.ANY, MaxImageCount = 20 },
            ],
        });
    }

    /// <summary>
    /// What the running API may do: read and write documents (never delete an original), use the analysis
    /// queue, send email through SES, and call Bedrock only when it is switched on.
    /// </summary>
    private Role CreateTaskRole(InfraSettings settings, ResourceNames names, DataStack data, MessagingStack messaging)
    {
        var role = new Role(this, "TaskRole", new RoleProps
        {
            RoleName = names.TaskRoleName,
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
        });

        AddDocumentPermissions(role, data);
        messaging.Queue.GrantSendMessages(role);
        messaging.Queue.GrantConsumeMessages(role);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "SendEmail",
            Actions = ["ses:SendEmail", "ses:SendRawEmail"],
            Resources = [$"arn:aws:ses:{InfraSettings.Region}:{settings.Account}:identity/{settings.EmailIdentity}"],
        }));

        if (settings.EnableBedrock)
        {
            AddBedrockPermission(role, settings);
        }

        return role;
    }

    /// <summary>Read and write documents, but never delete anything under <c>originals/</c> (an explicit deny always wins).</summary>
    private static void AddDocumentPermissions(Role role, DataStack data)
    {
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ReadAndWriteDocuments",
            Actions = ["s3:GetObject", "s3:GetObjectVersion", "s3:PutObject"],
            Resources = [data.DocumentsBucket.ArnForObjects("*")],
        }));
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ListDocuments",
            Actions = ["s3:ListBucket"],
            Resources = [data.DocumentsBucket.BucketArn],
        }));
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "NeverDeleteOriginals",
            Effect = Effect.DENY,
            Actions = ["s3:DeleteObject", "s3:DeleteObjectVersion"],
            Resources = [data.DocumentsBucket.ArnForObjects("originals/*")],
        }));
    }

    /// <summary>
    /// Bedrock's global inference profile can run the model in any region, so the model ARN uses "*" for the
    /// region. This processes data outside South Africa, which is why it is off unless enableBedrock=true.
    /// </summary>
    private static void AddBedrockPermission(Role role, InfraSettings settings)
    {
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "InvokeBedrockModel",
            Actions = ["bedrock:InvokeModel"],
            Resources =
            [
                settings.BedrockInferenceProfileArn!,
                $"arn:aws:bedrock:*::foundation-model/{settings.BedrockModelId}",
            ],
        }));
    }

    /// <summary>
    /// One container on ARM64 (Graviton). Settings come in as environment variables; secrets (database
    /// password, JWT key, origin secret) are injected by ECS and never appear in the task definition.
    /// </summary>
    private FargateTaskDefinition CreateTaskDefinition(
        InfraSettings settings,
        ResourceNames names,
        IRepository repository,
        IRole taskRole,
        DataStack data,
        MessagingStack messaging)
    {
        var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
        {
            Family = names.TaskFamily,
            Cpu = 512,
            MemoryLimitMiB = 1024,
            RuntimePlatform = new RuntimePlatform
            {
                CpuArchitecture = CpuArchitecture.ARM64,
                OperatingSystemFamily = OperatingSystemFamily.LINUX,
            },
            TaskRole = taskRole,
            ExecutionRole = CreateExecutionRole(names),
        });

        taskDefinition.AddContainer(ResourceNames.ContainerName, new ContainerDefinitionOptions
        {
            ContainerName = ResourceNames.ContainerName,
            // Before the first backend deploy there is no image yet; the service then runs zero tasks.
            Image = ContainerImage.FromEcrRepository(repository, settings.ImageTag ?? "not-deployed-yet"),
            PortMappings = [new PortMapping { ContainerPort = ContainerPort }],
            // A small init process runs as process 1, passing stop signals to the API and letting it exit cleanly.
            LinuxParameters = new LinuxParameters(this, "LinuxParameters", new LinuxParametersProps { InitProcessEnabled = true }),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps { LogGroup = CreateLogGroup(names), StreamPrefix = "api" }),
            Environment = ContainerSettings(settings, data, messaging),
            Secrets = ContainerSecrets(names, data),
        });

        return taskDefinition;
    }

    /// <summary>Used by ECS itself to pull the image, write logs and read the secrets at start-up.</summary>
    private Role CreateExecutionRole(ResourceNames names)
    {
        return new Role(this, "ExecutionRole", new RoleProps
        {
            RoleName = names.ExecutionRoleName,
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            ManagedPolicies = [ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy")],
        });
    }

    /// <summary>The API's logs, kept for 30 days.</summary>
    private LogGroup CreateLogGroup(ResourceNames names)
    {
        return new LogGroup(this, "LogGroup", new LogGroupProps
        {
            LogGroupName = names.LogGroupName,
            Retention = RetentionDays.ONE_MONTH,
            RemovalPolicy = RemovalPolicy.RETAIN,
        });
    }

    /// <summary>
    /// Plain (non-secret) settings for the API, as environment variables. Email goes out through SES from
    /// <c>no-reply@&lt;domain&gt;</c>, or from the sender address when there is no domain.
    /// </summary>
    private static Dictionary<string, string> ContainerSettings(InfraSettings settings, DataStack data, MessagingStack messaging)
    {
        return new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Swagger__Enabled"] = settings.EnableSwagger ? "true" : "false",
            ["Aws__Region"] = InfraSettings.Region,
            ["Admin__Emails"] = settings.AdminEmails ?? string.Empty,
            ["Email__Provider"] = "Ses",
            ["Email__FromAddress"] = settings.EmailFromAddress,
            ["Storage__DocumentsBucket"] = data.DocumentsBucket.BucketName,
            ["Queues__DocumentAnalysisUrl"] = messaging.Queue.QueueUrl,
        };
    }

    /// <summary>
    /// The database settings come from the RDS secret's fields; the app joins them into a connection string.
    /// The JWT key is an SSM SecureString created by hand (CloudFormation cannot create SecureStrings).
    /// The app's address, for links in emails, is the parameter the web stack writes once CloudFront exists.
    /// It is looked up by name so this stack does not depend on the web stack.
    /// </summary>
    private Dictionary<string, EcsSecret> ContainerSecrets(ResourceNames names, DataStack data)
    {
        var databaseSecret = data.Database.Secret!;
        var jwtSigningKey = StringParameter.FromSecureStringParameterAttributes(this, "JwtSigningKey", new SecureStringParameterAttributes
        {
            ParameterName = names.JwtSigningKeyParameterName,
        });

        var appBaseUrl = StringParameter.FromStringParameterName(this, "AppBaseUrl", names.AppBaseUrlParameterName);

        return new Dictionary<string, EcsSecret>
        {
            ["App__BaseUrl"] = EcsSecret.FromSsmParameter(appBaseUrl),
            ["Database__Host"] = EcsSecret.FromSecretsManager(databaseSecret, "host"),
            ["Database__Port"] = EcsSecret.FromSecretsManager(databaseSecret, "port"),
            ["Database__Name"] = EcsSecret.FromSecretsManager(databaseSecret, "dbname"),
            ["Database__Username"] = EcsSecret.FromSecretsManager(databaseSecret, "username"),
            ["Database__Password"] = EcsSecret.FromSecretsManager(databaseSecret, "password"),
            ["Jwt__SigningKey"] = EcsSecret.FromSsmParameter(jwtSigningKey),
            ["OriginVerify__Secret"] = EcsSecret.FromSecretsManager(OriginVerifySecret),
        };
    }

    /// <summary>
    /// One task in a public subnet with a public IP (no NAT Gateway needed). If a new version fails its health
    /// checks, ECS rolls back to the previous one automatically.
    /// </summary>
    private FargateService CreateService(InfraSettings settings, ResourceNames names, IVpc vpc, FargateTaskDefinition taskDefinition)
    {
        var cluster = new Cluster(this, "Cluster", new ClusterProps { ClusterName = names.ClusterName, Vpc = vpc });

        return new FargateService(this, "Service", new FargateServiceProps
        {
            ServiceName = names.ServiceName,
            Cluster = cluster,
            TaskDefinition = taskDefinition,
            DesiredCount = settings.ImageTag is null ? 0 : 1,
            AssignPublicIp = true,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true },
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200,
            HealthCheckGracePeriod = Duration.Seconds(60),
        });
    }

    /// <summary>
    /// Internal load balancer in the isolated load-balancer subnets, sending requests to the API tasks. It has no
    /// public address and only CloudFront can reach it. Plain HTTP is fine here: the traffic never leaves AWS.
    /// </summary>
    private ApplicationLoadBalancer CreateLoadBalancer(InfraSettings settings, IVpc vpc, FargateService service)
    {
        var loadBalancer = new ApplicationLoadBalancer(this, "LoadBalancer", new ApplicationLoadBalancerProps
        {
            Vpc = vpc,
            InternetFacing = false,
            VpcSubnets = new SubnetSelection { SubnetGroupName = NetworkStack.LoadBalancerSubnets },
            SecurityGroup = CreateLoadBalancerSecurityGroup(settings, vpc),
        });

        AddHttpListener(loadBalancer, service);
        return loadBalancer;
    }

    /// <summary>
    /// Lets in HTTP from CloudFront's own addresses only (the AWS-managed origin-facing prefix list, which
    /// also covers VPC origins), so nothing else in the VPC can call the API through the load balancer.
    /// </summary>
    private SecurityGroup CreateLoadBalancerSecurityGroup(InfraSettings settings, IVpc vpc)
    {
        var securityGroup = new SecurityGroup(this, "LoadBalancerSecurityGroup", new SecurityGroupProps
        {
            Vpc = vpc,
            Description = "HTTP from CloudFront only",
            AllowAllOutbound = false,
        });
        securityGroup.AddIngressRule(Peer.PrefixList(settings.CloudFrontPrefixListId), Port.Tcp(80), "CloudFront origin-facing addresses");
        return securityGroup;
    }

    /// <summary>HTTP on 80, forwarding to the API on port 8080 and checking it through <c>/api/v1/health</c>.</summary>
    private static void AddHttpListener(ApplicationLoadBalancer loadBalancer, FargateService service)
    {
        var listener = loadBalancer.AddListener("Http", new BaseApplicationListenerProps
        {
            Port = 80,
            Protocol = ApplicationProtocol.HTTP,
            Open = false,
        });

        listener.AddTargets("Api", new AddApplicationTargetsProps
        {
            Port = ContainerPort,
            Protocol = ApplicationProtocol.HTTP,
            Targets = [service],
            DeregistrationDelay = Duration.Seconds(30),
            HealthCheck = new HealthCheck
            {
                Path = "/api/v1/health",
                HealthyHttpCodes = "200",
                Interval = Duration.Seconds(30),
                Protocol = Protocol.HTTP,
            },
        });
    }
}
