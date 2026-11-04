using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF command-line tools (for example <c>dotnet ef migrations add</c>), never by the app.
/// Creating a migration does not connect to the database, so the connection string here is a local placeholder.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TumiiaDbContext>
{
    private const string LocalConnectionString = "Host=localhost;Port=5432;Database=tumiia;Username=tumiia;Password=tumiia";

    /// <summary>Builds a context for the EF tools.</summary>
    public TumiiaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TumiiaDbContext>();
        TumiiaDbContextOptions.Configure(options, LocalConnectionString);
        return new TumiiaDbContext(options.Options);
    }
}
