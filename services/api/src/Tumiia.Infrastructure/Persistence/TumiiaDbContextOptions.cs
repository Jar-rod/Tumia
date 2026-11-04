using Microsoft.EntityFrameworkCore;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// The one place that says how TUMIIA talks to PostgreSQL, so the app and the EF command-line tools
/// always use the same settings.
/// </summary>
public static class TumiiaDbContextOptions
{
    /// <summary>
    /// Points EF Core at PostgreSQL and turns on snake_case names for tables and columns.
    /// Migrations live in this (Infrastructure) project.
    /// </summary>
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(TumiiaDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention();
    }
}
