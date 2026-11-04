using Microsoft.Extensions.Configuration;
using Npgsql;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.UnitTests.Persistence;

public class DatabaseConnectionStringTests
{
    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Resolve_WhenFullConnectionStringIsSet_ReturnsItUnchanged()
    {
        var configuration = Configuration(new()
        {
            ["ConnectionStrings:Tumiia"] = "Host=localhost;Database=tumiia",
            ["Database:Host"] = "ignored.example.com",
        });

        var connectionString = DatabaseConnectionString.Resolve(configuration);

        connectionString.ShouldBe("Host=localhost;Database=tumiia");
    }

    [Fact]
    public void Resolve_WhenOnlyPartsAreSet_BuildsEncryptedConnectionFromThem()
    {
        var configuration = Configuration(new()
        {
            ["Database:Host"] = "db.example.com",
            ["Database:Port"] = "5433",
            ["Database:Name"] = "tumiia",
            ["Database:Username"] = "app",
            ["Database:Password"] = "p@ss;word",
        });

        var connectionString = DatabaseConnectionString.Resolve(configuration);

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        parsed.Host.ShouldBe("db.example.com");
        parsed.Port.ShouldBe(5433);
        parsed.Database.ShouldBe("tumiia");
        parsed.Username.ShouldBe("app");
        parsed.Password.ShouldBe("p@ss;word");
        parsed.SslMode.ShouldBe(SslMode.Require);
    }

    [Fact]
    public void Resolve_WhenPortIsMissing_UsesPostgresDefaultPort()
    {
        var configuration = Configuration(new() { ["Database:Host"] = "db.example.com" });

        var connectionString = DatabaseConnectionString.Resolve(configuration);

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        parsed.Port.ShouldBe(5432);
    }

    [Fact]
    public void Resolve_WhenNothingIsConfigured_ReturnsNull()
    {
        var configuration = Configuration(new());

        var connectionString = DatabaseConnectionString.Resolve(configuration);

        connectionString.ShouldBeNull();
    }
}
