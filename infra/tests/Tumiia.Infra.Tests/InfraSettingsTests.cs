using Amazon.CDK;

namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class InfraSettingsTests
{
    private static InfraSettings Read(Dictionary<string, object> context)
    {
        var app = new App(new AppProps { Context = context });
        return InfraSettings.FromContext(app.Node);
    }

    [Fact]
    public void FromContext_WhenRequiredSettingIsMissing_SaysWhichOneAndHowToPassIt()
    {
        var context = SynthesizedStacks.Context(new());
        context.Remove("alertEmail");

        var error = Should.Throw<ArgumentException>(() => Read(context));

        error.Message.ShouldContain("-c alertEmail=");
    }

    [Fact]
    public void FromContext_WhenBedrockIsOnWithoutAModel_Refuses()
    {
        var context = SynthesizedStacks.Context(new() { ["enableBedrock"] = "true" });

        var error = Should.Throw<ArgumentException>(() => Read(context));

        error.Message.ShouldContain("bedrockModelId");
    }

    [Fact]
    public void FromContext_WhenOptionalSettingsAreMissing_UsesSafeDefaults()
    {
        var context = SynthesizedStacks.Context(new());
        context.Remove("hostedZoneName");

        var settings = Read(context);

        settings.EnvironmentName.ShouldBe("prod");
        settings.HostedZoneName.ShouldBe(SynthesizedStacks.DomainName);
        settings.ImageTag.ShouldBeNull();
        settings.EnableBedrock.ShouldBeFalse();
    }
}
