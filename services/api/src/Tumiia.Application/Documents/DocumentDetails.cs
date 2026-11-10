using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.Application.Documents;

/// <summary>A document and its newest analysis (null until it has been analysed).</summary>
/// <param name="Document">The document.</param>
/// <param name="LatestAnalysis">Its newest analysis, if any.</param>
public sealed record DocumentDetails(Document Document, DocumentAnalysis? LatestAnalysis);
