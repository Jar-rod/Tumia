using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>Document analyses, which are only ever added, never changed. Implemented with EF Core in Infrastructure.</summary>
public interface IDocumentAnalysisStore
{
    /// <summary>True when the document already has an analysis from this pipeline version.</summary>
    Task<bool> ExistsAsync(Guid documentId, string pipelineVersion, CancellationToken cancellationToken);

    /// <summary>An analysis by id, or null.</summary>
    Task<DocumentAnalysis?> FindAsync(Guid analysisId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the analysis and saves it together with the changes made to its document (loaded from the
    /// <c>IDocumentStore</c> in the same request), so the two are never out of step.
    /// </summary>
    Task AddWithDocumentAsync(DocumentAnalysis analysis, CancellationToken cancellationToken);
}
