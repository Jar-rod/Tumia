namespace Tumiia.Domain.Analysis;

/// <summary>Everything the interpreter needs about one document.</summary>
/// <param name="Text">What was read from the file locally (typed text, EXIF, CAD facts).</param>
/// <param name="Format">The file's real format.</param>
/// <param name="FileName">The original file name (it often says what the file is).</param>
/// <param name="Categories">The taxonomy.</param>
/// <param name="Hosted">What the hosted AI said, or null when it was not used (switched off, or it failed).</param>
/// <param name="Weights">The confidence weights.</param>
public sealed record InterpretationRequest(
    DocumentText Text,
    FileFormat Format,
    string FileName,
    IReadOnlyList<TaxonomyCategory> Categories,
    HostedAnalysis? Hosted,
    ConfidenceWeights Weights);
