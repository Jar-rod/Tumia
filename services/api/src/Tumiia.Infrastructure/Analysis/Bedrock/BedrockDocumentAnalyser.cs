using Amazon.Runtime;
using Anthropic;
using Anthropic.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>
/// Reads and classifies documents with Claude on Amazon Bedrock, through the Bedrock Messages API endpoint
/// (<c>bedrock-mantle</c>), signed with the task role's AWS credentials. The SDK retries throttling and server errors
/// twice; if the call still fails (or no AWS credentials can be found), this returns no analysis and a warning, so
/// the keyword rules take over.
/// </summary>
public sealed class BedrockDocumentAnalyser(IAnthropicClient client, IOptions<BedrockSettings> settings, ILogger<BedrockDocumentAnalyser> logger) : IHostedDocumentAnalyser
{
    /// <summary>The warning recorded when Bedrock could not be reached or answered with an error.</summary>
    public const string Unavailable = "The AI service could not be reached, so this was sorted by keywords only.";

    /// <inheritdoc />
    public async Task<HostedAnalysisResult> AnalyseAsync(DocumentContent content, IReadOnlyList<TaxonomyCategory> categories, CancellationToken cancellationToken)
    {
        var request = BedrockRequestBuilder.Build(content, categories, settings.Value);
        if (request.Parameters is null)
        {
            return new HostedAnalysisResult(null, request.Problem);
        }

        try
        {
            var message = await client.Messages.Create(request.Parameters, cancellationToken);
            var (analysis, problem) = BedrockResponseReader.Read(message, settings.Value.ModelId);
            if (analysis is not null && request.Note is not null)
            {
                analysis = analysis with { Warnings = [.. analysis.Warnings, request.Note] };
            }

            return new HostedAnalysisResult(analysis, problem);
        }
        catch (Exception exception) when (IsServiceFailure(exception, cancellationToken))
        {
            logger.LogWarning(exception, "Bedrock did not analyse a {Format} document", content.Format.Format);
            return new HostedAnalysisResult(null, Unavailable);
        }
    }

    // API errors, network errors, timeouts, and missing or expired AWS credentials (raised while signing the call).
    private static bool IsServiceFailure(Exception exception, CancellationToken cancellationToken)
    {
        return exception is AnthropicException or HttpRequestException or AmazonClientException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    }
}
