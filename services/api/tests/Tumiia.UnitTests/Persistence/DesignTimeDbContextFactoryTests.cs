using Microsoft.EntityFrameworkCore;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.UnitTests.Persistence;

public class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WhenCalledByEfTools_UsesPostgreSql()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        context.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
}
