using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tumiia.Domain.Email;
using Tumiia.Infrastructure;

namespace Tumiia.IntegrationTests;

/// <summary>
/// Starts the real API in memory, backed by a real PostgreSQL 17 database in a Docker container.
/// The app runs as Development, so it applies the migrations to the container on start-up.
/// Rate limits are raised so tests do not trip each other; the rate-limit test puts them back.
/// </summary>
public sealed class TumiiaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Gets the Admin role through <c>Admin:Emails</c>.</summary>
    public const string AdminEmail = "admin@tumiia.test";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    /// <summary>Emails the app "sent" (nothing really goes out).</summary>
    public CapturedEmails Emails { get; } = new();

    /// <summary>Starts the database container before any test runs.</summary>
    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
    }

    /// <summary>A client on https://localhost (so the Secure refresh cookie works). Cookies are handled by the tests.</summary>
    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
    }

    /// <summary>Points the app at the test database, raises the rate limits and captures emails.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _database.GetConnectionString());
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Default:PermitLimit", "100000");
        builder.UseSetting("Admin:Emails", AdminEmail);
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Emails));
    }

    /// <summary>Stops the app, then removes the database container.</summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
