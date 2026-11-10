using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>The filing categories, from the database. Implemented with EF Core in Infrastructure.</summary>
public interface ITaxonomyStore
{
    /// <summary>Every category, including Needs Review.</summary>
    Task<IReadOnlyList<TaxonomyCategory>> ListAsync(CancellationToken cancellationToken);
}
