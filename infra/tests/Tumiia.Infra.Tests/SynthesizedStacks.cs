using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.CDK;
using Amazon.CDK.Assertions;

namespace Tumiia.Infra.Tests;

/// <summary>
/// Builds the stacks once with test settings so every test can inspect the CloudFormation they produce.
/// No AWS account is needed.
/// </summary>
public sealed class SynthesizedStacks
{
    public const string Repo = "Jar-rod@121164104/Tumia@987654321";
    public const string DomainName = "app.example.co.za";
    public const string PrefixListId = "pl-12345678";
    public const string AlertEmail = "alerts@example.co.za";

    /// <summary>Stacks as on the very first deploy: no domain, no image yet, Bedrock off.</summary>
    public TumiiaStacks BeforeFirstDeploy { get; } = Create(new Dictionary<string, object>());

    /// <summary>Stacks with the app's own domain in Route 53.</summary>
    public TumiiaStacks WithDomain { get; } = Create(DomainSettings());

    /// <summary>Stacks once an image is deployed, with Bedrock switched on.</summary>
    public TumiiaStacks WithImageAndBedrock { get; } = Create(new Dictionary<string, object>
    {
        ["imageTag"] = "abc1234",
        ["adminEmails"] = "owner@example.co.za",
        ["enableBedrock"] = "true",
        ["bedrockInferenceProfileArn"] = "arn:aws:bedrock:af-south-1:123456789012:inference-profile/global.example",
        ["bedrockModelId"] = "example.model-v1",
    });

    /// <summary>The settings that give the app its own domain.</summary>
    public static Dictionary<string, object> DomainSettings()
    {
        return new Dictionary<string, object>
        {
            ["domainName"] = DomainName,
            ["hostedZoneName"] = "example.co.za",
            ["hostedZoneId"] = "Z0123456789ABCDEFGHIJ",
        };
    }

    /// <summary>The usual settings (no domain), plus any extra context the test needs.</summary>
    public static Dictionary<string, object> Context(Dictionary<string, object> extra)
    {
        var context = new Dictionary<string, object>
        {
            ["account"] = "123456789012",
            ["githubRepo"] = Repo,
            ["alertEmail"] = AlertEmail,
            ["cloudFrontPrefixListId"] = PrefixListId,
        };

        foreach (var (key, value) in extra)
        {
            context[key] = value;
        }

        return context;
    }

    /// <summary>The stack's CloudFormation template as JSON, for checks the assertion library cannot express.</summary>
    public static JsonNode Json(Stack stack)
    {
        return JsonNode.Parse(JsonSerializer.Serialize(Template.FromStack(stack).ToJSON()))!;
    }

    /// <summary>Every resource of one CloudFormation type in the template.</summary>
    public static List<JsonNode> Resources(JsonNode template, string type)
    {
        return template["Resources"]!.AsObject()
            .Select(resource => resource.Value!)
            .Where(resource => resource["Type"]!.GetValue<string>() == type)
            .ToList();
    }

    private static TumiiaStacks Create(Dictionary<string, object> extra)
    {
        var app = new App(new AppProps { Context = Context(extra) });
        return TumiiaStacks.Create(app, InfraSettings.FromContext(app.Node));
    }
}
