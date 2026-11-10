using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tumiia.Infrastructure.Storage;

namespace Tumiia.Infrastructure.Analysis;

/// <summary>
/// Reads the analysis queue in the background, inside the API process, one message at a time. It waits up to 20
/// seconds for each message (long polling), so an empty queue costs almost nothing. Switched off with
/// <c>Analysis:WorkerEnabled=false</c>, or when no queue is configured.
/// </summary>
public sealed class DocumentAnalysisWorker(
    IAmazonSQS sqs,
    AnalysisMessageHandler handler,
    IOptions<StorageSettings> storage,
    IOptions<AnalysisSettings> settings,
    ILogger<DocumentAnalysisWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PauseAfterError = TimeSpan.FromSeconds(5);

    /// <summary>Receives and handles messages until the app stops.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = storage.Value.DocumentAnalysisQueueUrl;
        if (!settings.Value.WorkerEnabled || string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogInformation("The document analysis worker is off");
            return;
        }

        logger.LogInformation("The document analysis worker is reading the queue");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReceiveOnceAsync(queueUrl, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not read the analysis queue; trying again shortly");
                await Task.Delay(PauseAfterError, stoppingToken);
            }
        }
    }

    /// <summary>Waits for at most one message and handles it.</summary>
    public async Task ReceiveOnceAsync(string queueUrl, CancellationToken cancellationToken)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20,
            MessageSystemAttributeNames = [AnalysisMessageHandler.ReceiveCountAttribute],
        };
        var response = await sqs.ReceiveMessageAsync(request, cancellationToken);
        foreach (var message in response.Messages ?? [])
        {
            await handler.HandleAsync(message, cancellationToken);
        }
    }
}
