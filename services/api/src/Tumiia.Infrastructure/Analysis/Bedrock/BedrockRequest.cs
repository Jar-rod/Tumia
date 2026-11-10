using Anthropic.Models.Messages;

namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>A request ready to send, or why the document can't be sent.</summary>
/// <param name="Parameters">The request, or null when the document can't be sent.</param>
/// <param name="Problem">Why it can't be sent, in plain words, or null.</param>
/// <param name="Note">Something to record with the analysis even though it was sent (for example, text was cut), or null.</param>
public sealed record BedrockRequest(MessageCreateParams? Parameters, string? Problem, string? Note);
