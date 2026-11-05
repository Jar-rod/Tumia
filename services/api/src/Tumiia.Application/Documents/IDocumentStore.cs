using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>Documents. Implemented with EF Core in Infrastructure.</summary>
public interface IDocumentStore
{
    /// <summary>A document by id, or null.</summary>
    Task<Document?> FindAsync(Guid documentId, CancellationToken cancellationToken);

    /// <summary>The document in this project with this SHA-256, or null.</summary>
    Task<Document?> FindByHashAsync(Guid projectId, string sha256, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new document. If another request saved the same file (project and hash) a moment earlier, nothing is
    /// saved and that existing document is returned instead.
    /// </summary>
    Task<Document> AddOrGetExistingAsync(Document document, CancellationToken cancellationToken);

    /// <summary>Saves changes made to a document loaded from this store.</summary>
    Task SaveAsync(Document document, CancellationToken cancellationToken);

    /// <summary>A project's documents, newest first, optionally only those with a status or a confirmed category.</summary>
    Task<IReadOnlyList<Document>> ListAsync(Guid projectId, DocumentStatus? status, string? category, CancellationToken cancellationToken);
}
