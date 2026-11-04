using System.Text.Json.Nodes;
using Amazon.CDK.Assertions;

namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class AppTests(SynthesizedStacks stacks)
{
    private static List<JsonNode> PolicyStatements(JsonNode template)
    {
        return SynthesizedStacks.Resources(template, "AWS::IAM::Policy")
            .SelectMany(policy => policy["Properties"]!["PolicyDocument"]!["Statement"]!.AsArray())
            .Select(statement => statement!)
            .ToList();
    }

    [Fact]
    public void TaskDefinition_RunsOnArm64WithHalfAVcpuAndOneGigabyte()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.App);

        template.HasResourceProperties("AWS::ECS::TaskDefinition", Match.ObjectLike(new Dictionary<string, object>
        {
            ["Cpu"] = "512",
            ["Memory"] = "1024",
            ["RuntimePlatform"] = new Dictionary<string, object> { ["CpuArchitecture"] = "ARM64", ["OperatingSystemFamily"] = "LINUX" },
        }));
    }

    [Fact]
    public void Service_BeforeTheFirstImageIsDeployed_RunsNoTasks()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.App);

        template.HasResourceProperties("AWS::ECS::Service", Match.ObjectLike(new Dictionary<string, object> { ["DesiredCount"] = 0 }));
    }

    [Fact]
    public void Service_WithAnImage_RunsOneTaskAndRollsBackFailedDeployments()
    {
        var template = Template.FromStack(stacks.WithImageAndBedrock.App);

        template.HasResourceProperties("AWS::ECS::Service", Match.ObjectLike(new Dictionary<string, object>
        {
            ["DesiredCount"] = 1,
            ["DeploymentConfiguration"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["DeploymentCircuitBreaker"] = new Dictionary<string, object> { ["Enable"] = true, ["Rollback"] = true },
            }),
        }));
    }

    [Fact]
    public void LoadBalancer_OnlyAcceptsHttpsFromCloudFront()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App);

        var openToInternet = template.ToJsonString().Contains("\"CidrIp\":\"0.0.0.0/0\",\"Description\":\"Allow from anyone");
        var cloudFrontRules = SynthesizedStacks.Resources(template, "AWS::EC2::SecurityGroupIngress")
            .Where(rule => rule["Properties"]!["SourcePrefixListId"]?.GetValue<string>() == SynthesizedStacks.PrefixListId)
            .ToList();

        openToInternet.ShouldBeFalse();
        cloudFrontRules.ShouldHaveSingleItem()["Properties"]!["FromPort"]!.GetValue<int>().ShouldBe(443);
    }

    [Fact]
    public void TaskRole_CanNeverDeleteAnOriginalDocument()
    {
        var statements = PolicyStatements(SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App));

        var denyDelete = statements.SingleOrDefault(statement => statement["Sid"]?.GetValue<string>() == "NeverDeleteOriginals");

        denyDelete.ShouldNotBeNull();
        denyDelete["Effect"]!.GetValue<string>().ShouldBe("Deny");
        denyDelete["Action"]!.ToJsonString().ShouldContain("s3:DeleteObjectVersion");
        denyDelete["Resource"]!.ToJsonString().ShouldContain("/originals/*");
    }

    [Fact]
    public void TaskRole_ByDefault_CannotCallBedrock()
    {
        var policies = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App).ToJsonString();

        policies.ShouldNotContain("bedrock:");
    }

    [Fact]
    public void TaskRole_WhenBedrockIsEnabled_CanInvokeOnlyTheConfiguredModel()
    {
        var statements = PolicyStatements(SynthesizedStacks.Json(stacks.WithImageAndBedrock.App));

        var bedrock = statements.Single(statement => statement["Sid"]?.GetValue<string>() == "InvokeBedrockModel");

        bedrock["Action"]!.GetValue<string>().ShouldBe("bedrock:InvokeModel");
        bedrock["Resource"]!.ToJsonString().ShouldContain("foundation-model/example.model-v1");
    }

    [Fact]
    public void Container_GetsDatabasePasswordAndKeysAsSecretsNotPlainSettings()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App);
        var container = SynthesizedStacks.Resources(template, "AWS::ECS::TaskDefinition").Single()["Properties"]!["ContainerDefinitions"]![0]!;

        var secretNames = container["Secrets"]!.AsArray().Select(secret => secret!["Name"]!.GetValue<string>()).ToList();
        var plainSettings = container["Environment"]!.ToJsonString();

        secretNames.ShouldBe(["Database__Host", "Database__Port", "Database__Name", "Database__Username", "Database__Password", "Jwt__SigningKey", "OriginVerify__Secret"], ignoreOrder: true);
        plainSettings.ShouldNotContain("Password");
        plainSettings.ShouldNotContain("Secret");
    }
}
