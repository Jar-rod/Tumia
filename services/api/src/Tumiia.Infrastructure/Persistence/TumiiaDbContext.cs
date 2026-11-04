using Microsoft.EntityFrameworkCore;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for TUMIIA. Tables are added here phase by phase.
/// Table and column names are snake_case (set in <see cref="DependencyInjection"/>), to suit PostgreSQL.
/// </summary>
public sealed class TumiiaDbContext(DbContextOptions<TumiiaDbContext> options) : DbContext(options)
{
}
