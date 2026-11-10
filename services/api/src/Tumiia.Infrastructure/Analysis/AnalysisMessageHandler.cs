using System.Globalization;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tumiia.Application.Analysis;
using Tumiia.Infrastructure.Storage;

namespace Tumiia.Infrastructure.Analysis;

/// <summary>
/// Handles one analysis message from the queue. The message is deleted only after the result is saved. When the
/// analysis throws, the message is left on the queue to come back for another try; on the last try the document is
/// marked Failed, and SQS then moves the message to the dead-letter queue (which raises an alarm in AWS).
/// </summary>
public sealed class AnalysisMessageHandler(
    IAmazonSQS sqs,
    IServiceScopeFactory scopes,
    IOptions<StorageSettings> storage,
    IOptions<AnalysisSettings> settings,
    ILogger<AnalysisMessageHandler> logger)
{
    /// <summary>The SQS attribute that counts how often a message has been received.</summary>
    public const string ReceiveCountAttribute = "ApproximateReceiveCount";

    /// <summary>Analyses the document named in <paramref name="message"/>.</summary>
    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var documentId = ReadDocumentId(message.Body);
        if (documentId is null)
        {
            logger.LogWarning("Deleting an analysis message that names no document (message {MessageId})", message.MessageId);
            await DeleteAsync(message, cancellationToken);
            return;
        }

        try
        {
            using var scope = scopes.CreateScope();
            var outcome = await scope.ServiceProvider.GetRequiredService<AnalyseDocument>().ExecuteAsync(documentId.Value, cancellationToken);
            logger.LogInformation("Analysis of document {DocumentId}: {Outcome}", documentId, outcome);
            await DeleteAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var attempt = ReceiveCount(message);
            logger.LogError(exception, "Analysis of document {DocumentId} failed on try {Attempt} of {MaxAttempts}", documentId, attempt, settings.Value.MaxAttempts);
            if (attempt >= settings.Value.MaxAttempts)
            {
                await MarkFailedAsync(documentId.Value, cancellationToken);
            }
        }
    }

    /// <summary>The document id in a <c>{ "documentId": "..." }</c> message, or null when the body is not one.</summary>
    public static Guid? ReadDocumentId(string? body)
    {
        try
        {
            var request = JsonSerializer.Deserialize<AnalysisRequestMessage>(body ?? string.Empty, JsonSerializerOptions.Web);
            return request is null || request.DocumentId == Guid.Empty ? null : request.DocumentId;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>How many times SQS has handed out this message, including this time (1 when unknown).</summary>
    public static int ReceiveCount(Message message)
    {
        return message.Attributes?.TryGetValue(ReceiveCountAttribute, out var count) == true
            && int.TryParse(count, NumberStyles.Integer, CultureInfo.InvariantCulture, out var attempt)
            ? attempt
            : 1;
    }

    private async Task MarkFailedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var reason = $"TUMIIA could not analyse this file after {settings.Value.MaxAttempts} tries. A person needs to look at it.";
        await scope.ServiceProvider.GetRequiredService<FailDocumentAnalysis>().ExecuteAsync(documentId, reason, cancellationToken);
    }

    private Task DeleteAsync(Message message, CancellationToken cancellationToken)
    {
        return sqs.DeleteMessageAsync(storage.Value.DocumentAnalysisQueueUrl, message.ReceiptHandle, cancellationToken);
    }
}
