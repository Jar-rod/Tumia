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
    public void Container_RunsUnderAnInitProcess_SoItStopsAndExitsCleanly()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.App);

        template.HasResourceProperties("AWS::ECS::TaskDefinition", Match.ObjectLike(new Dictionary<string, object>
        {
            ["ContainerDefinitions"] = new object[]
            {
                Match.ObjectLike(new Dictionary<string, object>
                {
                    ["LinuxParameters"] = Match.ObjectLike(new Dictionary<string, object> { ["InitProcessEnabled"] = true }),
                }),
            },
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
    public void LoadBalancer_IsInternalWithNoPublicAddress()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.App);

        template.HasResourceProperties("AWS::ElasticLoadBalancingV2::LoadBalancer", Match.ObjectLike(new Dictionary<string, object>
        {
            ["Scheme"] = "internal",
        }));
    }

    [Fact]
    public void LoadBalancer_OnlyAcceptsTrafficFromCloudFront()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App);

        var openToInternet = template.ToJsonString().Contains("\"CidrIp\":\"0.0.0.0/0\",\"Description\":\"Allow from anyone");
        var cloudFrontRules = SynthesizedStacks.Resources(template, "AWS::EC2::SecurityGroupIngress")
            .Where(rule => rule["Properties"]!["SourcePrefixListId"]?.GetValue<string>() == SynthesizedStacks.PrefixListId)
            .ToList();

        openToInternet.ShouldBeFalse();
        cloudFrontRules.ShouldHaveSingleItem()["Properties"]!["FromPort"]!.GetValue<int>().ShouldBe(80);
    }

    [Fact]
    public void Stack_HasNoCertificateOrDnsRecord_BecauseTheLoadBalancerIsPrivate()
    {
        var template = Template.FromStack(stacks.WithDomain.App);

        template.ResourceCountIs("AWS::CertificateManager::Certificate", 0);
        template.ResourceCountIs("AWS::Route53::RecordSet", 0);
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

        policies.ShouldNotContain("bedrock-mantle:");
        policies.ShouldNotContain("bedrock:");
    }

    [Fact]
    public void TaskRole_WhenBedrockIsEnabled_MayOnlyCreateInferenceOnTheBedrockMessagesEndpoint()
    {
        var statements = PolicyStatements(SynthesizedStacks.Json(stacks.WithImageAndBedrock.App));

        var bedrock = statements.Single(statement => statement["Sid"]?.GetValue<string>() == "InvokeBedrockModel");

        bedrock["Action"]!.GetValue<string>().ShouldBe("bedrock-mantle:CreateInference");
    }

    [Fact]
    public void Container_WhenBedrockIsEnabled_ReadsDocumentsWithTheConfiguredModel()
    {
        var settings = ContainerEnvironment(stacks.WithImageAndBedrock);

        settings["Bedrock__Enabled"].ShouldBe("\"true\"");
        settings["Bedrock__Region"].ShouldBe("\"af-south-1\"");
        settings["Bedrock__ModelId"].ShouldBe("\"anthropic.example-model\"");
    }

    [Fact]
    public void Container_ByDefault_DoesNotSendDocumentsToBedrock()
    {
        ContainerEnvironment(stacks.BeforeFirstDeploy)["Bedrock__Enabled"].ShouldBe("\"false\"");
    }

    private static Dictionary<string, string> ContainerEnvironment(TumiiaStacks stacks)
    {
        var template = SynthesizedStacks.Json(stacks.App);
        var container = SynthesizedStacks.Resources(template, "AWS::ECS::TaskDefinition").Single()["Properties"]!["ContainerDefinitions"]![0]!;

        return container["Environment"]!.AsArray().ToDictionary(setting => setting!["Name"]!.GetValue<string>(), setting => setting!["Value"]!.ToJsonString());
    }

    [Fact]
    public void Container_WithoutADomain_SendsEmailThroughSesFromTheSenderAddress()
    {
        var settings = ContainerEnvironment(stacks.WithImageAndBedrock);

        settings["Email__Provider"].ShouldBe("\"Ses\"");
        settings["Email__FromAddress"].ShouldBe($"\"{SynthesizedStacks.AlertEmail}\"");
        settings["Admin__Emails"].ShouldBe("\"owner@example.co.za\"");
    }

    [Fact]
    public void Container_ByDefault_HasSwaggerOff()
    {
        var settings = ContainerEnvironment(stacks.BeforeFirstDeploy);

        settings["Swagger__Enabled"].ShouldBe("\"false\"");
    }

    [Fact]
    public void Container_WhenSwaggerIsEnabled_ServesSwagger()
    {
        var settings = ContainerEnvironment(stacks.WithImageAndBedrock);

        settings["Swagger__Enabled"].ShouldBe("\"true\"");
    }

    [Fact]
    public void Container_WithADomain_SendsEmailFromNoReplyOnTheDomain()
    {
        var settings = ContainerEnvironment(stacks.WithDomain);

        settings["Email__FromAddress"].ShouldBe($"\"no-reply@{SynthesizedStacks.DomainName}\"");
    }

    [Fact]
    public void TaskRole_CanOnlySendEmailAsTheSesIdentity()
    {
        var statements = PolicyStatements(SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App));

        var sendEmail = statements.Single(statement => statement["Sid"]?.GetValue<string>() == "SendEmail");

        sendEmail["Resource"]!.ToJsonString().ShouldEndWith($":identity/{SynthesizedStacks.AlertEmail}\"");
    }

    [Fact]
    public void Container_ReadsTheAppsAddressFromTheParameterTheWebStackWrites()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App);
        var container = SynthesizedStacks.Resources(template, "AWS::ECS::TaskDefinition").Single()["Properties"]!["ContainerDefinitions"]![0]!;

        var appBaseUrl = container["Secrets"]!.AsArray().Single(secret => secret!["Name"]!.GetValue<string>() == "App__BaseUrl")!;

        appBaseUrl["ValueFrom"]!.ToJsonString().ShouldContain("parameter/prod/tumiia/app-base-url");
    }

    [Fact]
    public void Container_GetsDatabasePasswordAndKeysAsSecretsNotPlainSettings()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.App);
        var container = SynthesizedStacks.Resources(template, "AWS::ECS::TaskDefinition").Single()["Properties"]!["ContainerDefinitions"]![0]!;

        var secretNames = container["Secrets"]!.AsArray().Select(secret => secret!["Name"]!.GetValue<string>()).ToList();
        var plainSettings = container["Environment"]!.ToJsonString();

        secretNames.ShouldBe(["App__BaseUrl", "Database__Host", "Database__Port", "Database__Name", "Database__Username", "Database__Password", "Jwt__SigningKey", "OriginVerify__Secret"], ignoreOrder: true);
        plainSettings.ShouldNotContain("Password");
        plainSettings.ShouldNotContain("Secret");
    }
}
