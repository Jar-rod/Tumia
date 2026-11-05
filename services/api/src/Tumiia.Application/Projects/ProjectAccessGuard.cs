using Tumiia.Application.Documents;
using Tumiia.Domain.Documents;

namespace Tumiia.Application.Projects;

/// <summary>
/// The one place that decides whether a user may see a project or a document. A project or document the user
/// cannot access is treated exactly like one that does not exist, so the API answers 404 and never reveals that
/// it is there.
/// </summary>
public sealed class ProjectAccessGuard(IProjectStore projects, IDocumentStore documents)
{
    /// <summary>True when the user is a member of the project.</summary>
    public async Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await projects.FindForMemberAsync(projectId, userId, cancellationToken) is not null;
    }

    /// <summary>The document, when it exists and the user is a member of its project; otherwise null.</summary>
    public async Task<Document?> FindAccessibleDocumentAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documents.FindAsync(documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        return await CanAccessProjectAsync(userId, document.ProjectId, cancellationToken) ? document : null;
    }
}
