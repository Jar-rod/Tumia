namespace Tumiia.Infrastructure.Storage;

/// <summary>The body of a message on the analysis queue.</summary>
public sealed record AnalysisRequestMessage(Guid DocumentId);
