using Tumiia.Application.Analysis;
using Tumiia.Application.Projects;

namespace Tumiia.Application.Documents;

/// <summary>Gets one document and its newest analysis, if the caller can access its project.</summary>
public sealed class GetDocument(ProjectAccessGuard access, IDocumentAnalysisStore analyses)
{
    /// <summary>The document with its analysis, or null.</summary>
    public async Task<DocumentDetails?> ExecuteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await access.FindAccessibleDocumentAsync(userId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var analysis = document.LatestAnalysisId is { } analysisId ? await analyses.FindAsync(analysisId, cancellationToken) : null;
        return new DocumentDetails(document, analysis);
    }
}
