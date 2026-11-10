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
    public void FromContext_WhenBedrockIsOnWithoutAModel_UsesClaudeHaiku()
    {
        var context = SynthesizedStacks.Context(new() { ["enableBedrock"] = "true" });

        var settings = Read(context);

        settings.EnableBedrock.ShouldBeTrue();
        settings.BedrockModelId.ShouldBe("anthropic.claude-haiku-5-5");
    }

    [Fact]
    public void FromContext_WhenOptionalSettingsAreMissing_UsesSafeDefaults()
    {
        var context = SynthesizedStacks.Context(new());

        var settings = Read(context);

        settings.EnvironmentName.ShouldBe("prod");
        settings.HasDomain.ShouldBeFalse();
        settings.ImageTag.ShouldBeNull();
        settings.EnableBedrock.ShouldBeFalse();
    }

    [Fact]
    public void FromContext_WithoutADomain_SendsEmailFromTheAlertAddress()
    {
        var settings = Read(SynthesizedStacks.Context(new()));

        settings.EmailFromAddress.ShouldBe(SynthesizedStacks.AlertEmail);
        settings.EmailIdentity.ShouldBe(SynthesizedStacks.AlertEmail);
    }

    [Fact]
    public void FromContext_WithoutADomain_UsesTheSenderEmailWhenGiven()
    {
        var settings = Read(SynthesizedStacks.Context(new() { ["senderEmail"] = "tumiia@example.co.za" }));

        settings.EmailFromAddress.ShouldBe("tumiia@example.co.za");
    }

    [Fact]
    public void FromContext_WithADomain_SendsEmailFromNoReplyOnTheDomain()
    {
        var settings = Read(SynthesizedStacks.Context(SynthesizedStacks.DomainSettings()));

        settings.EmailFromAddress.ShouldBe($"no-reply@{SynthesizedStacks.DomainName}");
        settings.EmailIdentity.ShouldBe(SynthesizedStacks.DomainName);
    }

    [Fact]
    public void FromContext_WithADomainButNoHostedZoneName_UsesTheDomainAsTheZoneName()
    {
        var context = SynthesizedStacks.Context(SynthesizedStacks.DomainSettings());
        context.Remove("hostedZoneName");

        var settings = Read(context);

        settings.HostedZoneName.ShouldBe(SynthesizedStacks.DomainName);
    }

    [Fact]
    public void FromContext_WithADomainButNoHostedZoneId_Refuses()
    {
        var context = SynthesizedStacks.Context(SynthesizedStacks.DomainSettings());
        context.Remove("hostedZoneId");

        var error = Should.Throw<ArgumentException>(() => Read(context));

        error.Message.ShouldContain("hostedZoneId");
    }
}
