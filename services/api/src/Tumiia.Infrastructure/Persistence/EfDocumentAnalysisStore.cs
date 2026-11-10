using Microsoft.EntityFrameworkCore;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>Analyses in the append-only <c>document_analyses</c> table.</summary>
public sealed class EfDocumentAnalysisStore(TumiiaDbContext database) : IDocumentAnalysisStore
{
    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid documentId, string pipelineVersion, CancellationToken cancellationToken)
    {
        return database.DocumentAnalyses.AnyAsync(analysis => analysis.DocumentId == documentId && analysis.PipelineVersion == pipelineVersion, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DocumentAnalysis?> FindAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        return database.DocumentAnalyses.AsNoTracking().SingleOrDefaultAsync(analysis => analysis.Id == analysisId, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The document is tracked by the same database context, so one SaveChanges stores both.</remarks>
    public async Task AddWithDocumentAsync(DocumentAnalysis analysis, CancellationToken cancellationToken)
    {
        database.DocumentAnalyses.Add(analysis);
        await database.SaveChangesAsync(cancellationToken);
    }
}
