using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.Infrastructure;

/// <summary>Registers the Infrastructure services with the app.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string in configuration (<c>ConnectionStrings:Tumiia</c>).</summary>
    public const string ConnectionStringName = "Tumiia";

    /// <summary>
    /// Adds the database context. The connection string is read when the context is first used,
    /// so the app can still start (for example to generate the OpenAPI file) without one.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TumiiaDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string 'ConnectionStrings:{ConnectionStringName}' is not set.");
            TumiiaDbContextOptions.Configure(options, connectionString);
        });

        return services;
    }

    /// <summary>
    /// Brings the database up to the latest migration. Only call this in Development:
    /// in AWS, migrations run as a separate one-off task before the new version starts.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>().Database;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TumiiaDbContext>>();

        logger.LogInformation("Applying database migrations");
        try
        {
            await database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogCritical(exception, "Could not apply database migrations. Is PostgreSQL running? From the repo root run: docker compose up -d");
            throw;
        }
    }
}
