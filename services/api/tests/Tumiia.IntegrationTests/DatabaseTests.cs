using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DatabaseTests(TumiiaApiFactory factory)
{
    [Fact]
    public async Task Startup_WhenRunningInDevelopment_AppliesAllMigrations()
    {
        _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>().Database;

        var pending = await database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        var applied = await database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        pending.ShouldBeEmpty();
        applied.ShouldContain(migration => migration.EndsWith("_InitialCreate"));
    }
}
