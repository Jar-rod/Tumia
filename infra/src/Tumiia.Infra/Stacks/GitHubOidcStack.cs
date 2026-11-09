using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// Lets GitHub Actions deploy without long-lived AWS keys. GitHub signs a short-lived token; AWS swaps it for
/// temporary credentials for the deploy role, but only for this repository's main branch or its "prod"
/// environment. Deploy this stack once from a laptop (see docs/deploy-setup.md).
/// </summary>
public sealed class GitHubOidcStack : Stack
{
    private const string GitHubTokenIssuer = "token.actions.githubusercontent.com";

    /// <summary>The role the deploy workflow assumes.</summary>
    public Role DeployRole { get; }

    /// <summary>Creates the GitHub identity provider and the deploy role with only the permissions deploys need.</summary>
    public GitHubOidcStack(Construct scope, string id, IStackProps props, InfraSettings settings, ResourceNames names)
        : base(scope, id, props)
    {
        var provider = new OidcProviderNative(this, "GitHubProvider", new OidcProviderNativeProps
        {
            Url = $"https://{GitHubTokenIssuer}",
            ClientIds = ["sts.amazonaws.com"],
        });

        DeployRole = new Role(this, "DeployRole", new RoleProps
        {
            RoleName = names.DeployRoleName,
            Description = "Assumed by GitHub Actions (deploy.yml) through OIDC.",
            AssumedBy = new WebIdentityPrincipal(provider.OidcProviderArn, TrustConditions(settings)),
            MaxSessionDuration = Duration.Hours(1),
        });

        AddCdkDeployPermissions(settings);
        AddImagePushPermissions(settings, names);
        AddEcsDeployPermissions(settings, names);
        AddPassRoleAndLogPermissions(settings, names);
        AddFrontendDeployPermissions(settings, names);
        AddStartStopPermissions(settings);

        _ = new CfnOutput(this, "DeployRoleArn", new CfnOutputProps { Value = DeployRole.RoleArn });
    }

    /// <summary>
    /// Only tokens for this repository, issued to the main branch or to the "prod" environment, are accepted.
    /// Deploy jobs run in the "prod" environment, which GitHub limits to the main branch (set in GitHub settings).
    /// </summary>
    private static Dictionary<string, object> TrustConditions(InfraSettings settings)
    {
        return new Dictionary<string, object>
        {
            ["StringEquals"] = new Dictionary<string, object>
            {
                [$"{GitHubTokenIssuer}:aud"] = "sts.amazonaws.com",
                [$"{GitHubTokenIssuer}:sub"] = new[]
                {
                    $"repo:{settings.GitHubRepo}:ref:refs/heads/main",
                    $"repo:{settings.GitHubRepo}:environment:{settings.EnvironmentName}",
                },
            },
        };
    }

    /// <summary>"cdk deploy" works by assuming the roles that "cdk bootstrap" created, so only that is needed here.</summary>
    private void AddCdkDeployPermissions(InfraSettings settings)
    {
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "AssumeCdkBootstrapRoles",
            Actions = ["sts:AssumeRole"],
            Resources = [$"arn:aws:iam::{settings.Account}:role/cdk-*"],
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ReadStackOutputs",
            Actions = ["cloudformation:DescribeStacks"],
            Resources = [$"arn:aws:cloudformation:{InfraSettings.Region}:{settings.Account}:stack/{settings.EnvironmentName}-tumiia-*/*"],
        }));
    }

    /// <summary>Log in to ECR and push images to the API's repository only.</summary>
    private void AddImagePushPermissions(InfraSettings settings, ResourceNames names)
    {
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "EcrLogin",
            Actions = ["ecr:GetAuthorizationToken"],
            Resources = ["*"],
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "EcrPush",
            Actions =
            [
                "ecr:BatchCheckLayerAvailability", "ecr:BatchGetImage", "ecr:CompleteLayerUpload", "ecr:DescribeImages",
                "ecr:InitiateLayerUpload", "ecr:PutImage", "ecr:UploadLayerPart",
            ],
            Resources = [$"arn:aws:ecr:{InfraSettings.Region}:{settings.Account}:repository/{names.RepositoryName}"],
        }));
    }

    /// <summary>Register task definitions, run the migrations task and roll out the service.</summary>
    private void AddEcsDeployPermissions(InfraSettings settings, ResourceNames names)
    {
        var region = InfraSettings.Region;
        var account = settings.Account;

        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "EcsTaskDefinitions",
            Actions = ["ecs:DescribeTaskDefinition", "ecs:RegisterTaskDefinition"],
            Resources = ["*"],
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "EcsDeploy",
            Actions = ["ecs:DescribeServices", "ecs:UpdateService", "ecs:RunTask", "ecs:DescribeTasks"],
            Resources =
            [
                $"arn:aws:ecs:{region}:{account}:service/{names.ClusterName}/{names.ServiceName}",
                $"arn:aws:ecs:{region}:{account}:task-definition/{names.TaskFamily}:*",
                $"arn:aws:ecs:{region}:{account}:task/{names.ClusterName}/*",
            ],
        }));
    }

    /// <summary>
    /// Hand the API's two roles to ECS (needed to register a task definition and run the migrations task), and
    /// read the migration task's logs when it fails.
    /// </summary>
    private void AddPassRoleAndLogPermissions(InfraSettings settings, ResourceNames names)
    {
        var account = settings.Account;

        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "PassTaskRolesToEcs",
            Actions = ["iam:PassRole"],
            Resources =
            [
                $"arn:aws:iam::{account}:role/{names.TaskRoleName}",
                $"arn:aws:iam::{account}:role/{names.ExecutionRoleName}",
            ],
            Conditions = new Dictionary<string, object>
            {
                ["StringEquals"] = new Dictionary<string, object> { ["iam:PassedToService"] = "ecs-tasks.amazonaws.com" },
            },
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ReadMigrationLogs",
            Actions = ["logs:GetLogEvents", "logs:FilterLogEvents"],
            Resources = [$"arn:aws:logs:{InfraSettings.Region}:{account}:log-group:{names.LogGroupName}:*"],
        }));
    }

    /// <summary>
    /// Stop and start this environment's database (the "Start or stop AWS" workflow, and deploys starting a
    /// stopped database first). Only databases tagged with this project and environment can be stopped or started.
    /// </summary>
    private void AddStartStopPermissions(InfraSettings settings)
    {
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ReadDatabaseStatus",
            Actions = ["rds:DescribeDBInstances"],
            Resources = [$"arn:aws:rds:{InfraSettings.Region}:{settings.Account}:db:*"],
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "StopAndStartDatabase",
            Actions = ["rds:StopDBInstance", "rds:StartDBInstance"],
            Resources = [$"arn:aws:rds:{InfraSettings.Region}:{settings.Account}:db:*"],
            Conditions = new Dictionary<string, object>
            {
                ["StringEquals"] = new Dictionary<string, object>
                {
                    ["aws:ResourceTag/project"] = "tumiia",
                    ["aws:ResourceTag/env"] = settings.EnvironmentName,
                },
            },
        }));
    }

    /// <summary>Upload the built React app to the web bucket and clear CloudFront's cache.</summary>
    private void AddFrontendDeployPermissions(InfraSettings settings, ResourceNames names)
    {
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "SyncWebBucket",
            Actions = ["s3:ListBucket", "s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
            Resources = [$"arn:aws:s3:::{names.WebBucketName}", $"arn:aws:s3:::{names.WebBucketName}/*"],
        }));
        DeployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "InvalidateCloudFront",
            Actions = ["cloudfront:CreateInvalidation", "cloudfront:GetInvalidation"],
            Resources = [$"arn:aws:cloudfront::{settings.Account}:distribution/*"],
        }));
    }
}
