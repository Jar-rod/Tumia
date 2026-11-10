using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// The taxonomy in the <c>taxonomy_categories</c> table. The first time it is read and the table is empty, it is
/// filled from the built-in seed; after that the table is the only source, so edits are never overwritten.
/// </summary>
public sealed class EfTaxonomyStore(TumiiaDbContext database, TimeProvider clock) : ITaxonomyStore
{
    private const string UniqueViolation = "23505";

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaxonomyCategory>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await ReadAsync(cancellationToken);
        if (records.Count == 0)
        {
            await SeedAsync(cancellationToken);
            records = await ReadAsync(cancellationToken);
        }

        return records.Select(record => AnalysisJson.Read<TaxonomyCategory>(record.Definition)).ToList();
    }

    private Task<List<TaxonomyCategoryRecord>> ReadAsync(CancellationToken cancellationToken)
    {
        return database.TaxonomyCategories.AsNoTracking().OrderBy(record => record.SortOrder).ToListAsync(cancellationToken);
    }

    // Two workers may seed at the same moment; the loser's insert fails on the key and it simply reads the winner's rows.
    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var records = TaxonomySeed.Load().Select((category, index) => new TaxonomyCategoryRecord
        {
            Key = category.Key,
            SortOrder = index,
            Definition = AnalysisJson.Write(category),
            UpdatedAt = now,
        });
        database.TaxonomyCategories.AddRange(records);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            database.ChangeTracker.Clear();
        }
    }
}
