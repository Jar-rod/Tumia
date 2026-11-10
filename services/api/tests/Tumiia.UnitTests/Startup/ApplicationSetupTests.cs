using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Tumiia.Api.Startup;

namespace Tumiia.UnitTests.Startup;

public class ApplicationSetupTests
{
    private static IHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void ReadAuthSettings_WhenAdminEmailsAreCommaSeparated_ReadsEachOne()
    {
        var configuration = Configuration(new() { ["App:BaseUrl"] = "https://app.test", ["Admin:Emails"] = "a@x.test, B@x.test ,," });

        var settings = ApplicationSetup.ReadAuthSettings(configuration, Environment(Environments.Production));

        settings.AdminEmails.Count.ShouldBe(2);
        settings.AdminEmails.Contains("b@X.TEST").ShouldBeTrue();
    }

    [Fact]
    public void ReadAuthSettings_WhenAdminEmailsAreAList_ReadsEachOne()
    {
        var configuration = Configuration(new() { ["App:BaseUrl"] = "https://app.test", ["Admin:Emails:0"] = "a@x.test", ["Admin:Emails:1"] = "b@x.test" });

        var settings = ApplicationSetup.ReadAuthSettings(configuration, Environment(Environments.Production));

        settings.AdminEmails.ShouldBe(["a@x.test", "b@x.test"], ignoreOrder: true);
    }

    [Fact]
    public void ReadAuthSettings_WhenBaseUrlIsMissingLocally_UsesTheViteDevServer()
    {
        var settings = ApplicationSetup.ReadAuthSettings(Configuration(new()), Environment(Environments.Development));

        settings.AppBaseUrl.ShouldBe("http://localhost:5173");
        settings.RefreshTokenLifetime.ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact]
    public void ReadAuthSettings_WhenBaseUrlIsMissingInProduction_Refuses()
    {
        var read = () => ApplicationSetup.ReadAuthSettings(Configuration(new()), Environment(Environments.Production));

        read.ShouldThrow<InvalidOperationException>().Message.ShouldContain("App:BaseUrl");
    }

    [Fact]
    public void ReadUploadRules_WhenExtensionsAreWrittenLoosely_NormalisesThem()
    {
        var configuration = Configuration(new()
        {
            ["Uploads:AllowedExtensions:0"] = "PDF",
            ["Uploads:AllowedExtensions:1"] = " .dwg ",
            ["Uploads:MaxFileSizeMb"] = "10",
        });

        var rules = ApplicationSetup.ReadUploadRules(configuration);

        rules.AllowedExtensions.ShouldBe([".pdf", ".dwg"], ignoreOrder: true);
        rules.MaxFileSizeMb.ShouldBe(10);
        rules.MaxFilesPerRequest.ShouldBe(50);
    }

    [Fact]
    public void ReadConfidenceWeights_WhenSomeWeightsAreSet_KeepsTheDefaultsForTheRest()
    {
        var configuration = Configuration(new() { ["Analysis:Weights:Agreement"] = "0.25" });

        var weights = ApplicationSetup.ReadConfidenceWeights(configuration);

        weights.Agreement.ShouldBe(0.25);
        weights.WinnerProbability.ShouldBe(0.30);
    }

    [Fact]
    public void ReadConfidenceWeights_WhenNoneAreSet_UsesTheBuildPlanDefaults()
    {
        ApplicationSetup.ReadConfidenceWeights(Configuration(new())).ShouldBe(new Tumiia.Domain.Analysis.ConfidenceWeights());
    }
}
