using Tumiia.Application.Projects;
using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>Lists a project's documents.</summary>
public sealed class ListDocuments(ProjectAccessGuard access, IDocumentStore documents)
{
    /// <summary>
    /// The documents, newest first, or null when the caller cannot access the project. <paramref name="category"/>
    /// matches the confirmed document type for now; once the analysis exists it also matches the proposed one.
    /// </summary>
    public async Task<IReadOnlyList<Document>?> ExecuteAsync(Guid userId, Guid projectId, DocumentStatus? status, string? category, CancellationToken cancellationToken)
    {
        if (!await access.CanAccessProjectAsync(userId, projectId, cancellationToken))
        {
            return null;
        }

        return await documents.ListAsync(projectId, status, category, cancellationToken);
    }
}
