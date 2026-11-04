using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// Used by the EF command-line tools and by the migrations bundle (<c>efbundle</c>), never by the app.
/// The connection comes from environment variables, as in AWS where the migrations task gets the same
/// <c>Database__*</c> variables as the app. With none set, it uses the local Docker database.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TumiiaDbContext>
{
    private const string LocalConnectionString = "Host=localhost;Port=5432;Database=tumiia;Username=tumiia;Password=tumiia";

    /// <summary>Builds a context for the EF tools or the migrations bundle.</summary>
    public TumiiaDbContext CreateDbContext(string[] args)
    {
        var environment = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connectionString = DatabaseConnectionString.Resolve(environment) ?? LocalConnectionString;

        var options = new DbContextOptionsBuilder<TumiiaDbContext>();
        TumiiaDbContextOptions.Configure(options, connectionString);
        return new TumiiaDbContext(options.Options);
    }
}
