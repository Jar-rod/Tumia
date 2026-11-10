namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>
/// The <c>Bedrock</c> configuration section: whether documents are read by Claude on Amazon Bedrock, which model,
/// and how. Off by default (locally and in tests); switched on in AWS. From af-south-1, Bedrock runs Claude through
/// its global endpoint, so documents are processed outside South Africa.
/// </summary>
public sealed class BedrockSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Bedrock";

    /// <summary>Whether to send documents to Bedrock. When false, the keyword rules classify them.</summary>
    public bool Enabled { get; set; }

    /// <summary>The AWS region whose Bedrock endpoint is called.</summary>
    public string Region { get; set; } = "af-south-1";

    /// <summary>The Bedrock model id.</summary>
    public string ModelId { get; set; } = "anthropic.claude-haiku-5-5";

    /// <summary>The most tokens the model may write (its thinking included). The answer itself is small JSON.</summary>
    public int MaxTokens { get; set; } = 8000;

    /// <summary>How hard the model thinks: low, medium, high or max. Low is enough for reading business documents.</summary>
    public string Effort { get; set; } = "low";

    /// <summary>The longest typed text sent, in characters. Longer text is cut, and the analysis says so.</summary>
    public int MaxTextCharacters { get; set; } = 200_000;

    /// <summary>The largest PDF sent, in megabytes (Bedrock takes 32 MB per request, and base64 adds a third).</summary>
    public int MaxPdfMegabytes { get; set; } = 20;

    /// <summary>Photos are shrunk so their longest side is at most this many pixels, which is all the model uses.</summary>
    public int MaxImageEdgePixels { get; set; } = 1568;
}
