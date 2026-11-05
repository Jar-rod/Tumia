using Tumiia.Application.Projects;
using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>Gets one document, if the caller can access its project.</summary>
public sealed class GetDocument(ProjectAccessGuard access)
{
    /// <summary>The document, or null.</summary>
    public Task<Document?> ExecuteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        return access.FindAccessibleDocumentAsync(userId, documentId, cancellationToken);
    }
}
