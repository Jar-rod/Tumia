using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Tumiia.Domain.Storage;

namespace Tumiia.Infrastructure.Storage;

/// <summary>Sends analysis requests to the SQS queue as <c>{ "documentId": "..." }</c>.</summary>
public sealed class SqsDocumentQueue(IAmazonSQS sqs, IOptions<StorageSettings> settings) : IDocumentQueue
{
    /// <summary>Queues one document. The message holds only the id; the worker reads everything else from the database.</summary>
    public async Task EnqueueForAnalysisAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new AnalysisRequestMessage(documentId), JsonSerializerOptions.Web);
        await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = settings.Value.DocumentAnalysisQueueUrl, MessageBody = body }, cancellationToken);
    }
}
