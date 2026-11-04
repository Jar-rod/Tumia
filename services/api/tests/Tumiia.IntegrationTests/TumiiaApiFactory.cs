using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Tumiia.Infrastructure;

namespace Tumiia.IntegrationTests;

/// <summary>
/// Starts the real API in memory, backed by a real PostgreSQL 17 database in a Docker container.
/// The app runs as Development, so it applies the migrations to the container on start-up.
/// </summary>
public sealed class TumiiaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    /// <summary>Starts the database container before any test runs.</summary>
    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
    }

    /// <summary>Points the app at the test database instead of the local one.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _database.GetConnectionString());
    }

    /// <summary>Stops the app, then removes the database container.</summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
