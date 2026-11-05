namespace Tumiia.Infrastructure.Storage;

/// <summary>Where documents and analysis requests go, from the <c>Storage</c> and <c>Queues</c> sections.</summary>
public sealed class StorageSettings
{
    /// <summary>The S3 bucket holding the original files (<c>Storage:DocumentsBucket</c>).</summary>
    public string DocumentsBucket { get; set; } = string.Empty;

    /// <summary>The SQS queue URL for analysis requests (<c>Queues:DocumentAnalysisUrl</c>).</summary>
    public string DocumentAnalysisQueueUrl { get; set; } = string.Empty;
}
