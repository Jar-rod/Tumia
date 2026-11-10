using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>Used when the hosted AI is switched off (<c>Bedrock:Enabled=false</c>, as locally and in tests): the keyword rules decide.</summary>
public sealed class NoHostedAnalyser : IHostedDocumentAnalyser
{
    /// <summary>Always "not used".</summary>
    public Task<HostedAnalysisResult> AnalyseAsync(DocumentContent content, IReadOnlyList<TaxonomyCategory> categories, CancellationToken cancellationToken)
    {
        return Task.FromResult(HostedAnalysisResult.NotUsed);
    }
}
