using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>
/// The hosted AI that reads and classifies documents: Claude on Amazon Bedrock. Implemented in Infrastructure.
/// It never throws for a document it cannot handle or a service that is down; it returns no analysis and a
/// warning, and the keyword rules take over.
/// </summary>
public interface IHostedDocumentAnalyser
{
    /// <summary>Asks the AI what <paramref name="content"/> is and what is in it, choosing from <paramref name="categories"/>.</summary>
    Task<HostedAnalysisResult> AnalyseAsync(DocumentContent content, IReadOnlyList<TaxonomyCategory> categories, CancellationToken cancellationToken);
}
