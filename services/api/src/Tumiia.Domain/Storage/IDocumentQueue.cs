namespace Tumiia.Domain.Storage;

/// <summary>The queue of documents waiting for analysis (Amazon SQS).</summary>
public interface IDocumentQueue
{
    /// <summary>Asks for one document to be analysed.</summary>
    Task EnqueueForAnalysisAsync(Guid documentId, CancellationToken cancellationToken);
}
