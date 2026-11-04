using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Tumiia.Api.Startup;

namespace Tumiia.UnitTests.Startup;

public class OpenApiSetupTests
{
    private static IHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }

    private static IConfiguration Configuration(string? swaggerEnabled)
    {
        var values = new Dictionary<string, string?>();
        if (swaggerEnabled is not null)
        {
            values["Swagger:Enabled"] = swaggerEnabled;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void IsSwaggerEnabled_WhenNotConfigured_IsOnOnlyInDevelopment(string environmentName, bool expected)
    {
        var enabled = OpenApiSetup.IsSwaggerEnabled(Configuration(null), Environment(environmentName));

        enabled.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Development", "false", false)]
    [InlineData("Production", "true", true)]
    public void IsSwaggerEnabled_WhenConfigured_FollowsConfiguration(string environmentName, string setting, bool expected)
    {
        var enabled = OpenApiSetup.IsSwaggerEnabled(Configuration(setting), Environment(environmentName));

        enabled.ShouldBe(expected);
    }
}
