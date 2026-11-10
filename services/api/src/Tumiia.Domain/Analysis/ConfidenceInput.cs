namespace Tumiia.Domain.Analysis;

/// <summary>What the confidence calculator needs to know about a document besides its signals.</summary>
/// <param name="Signals">The weighted signals.</param>
/// <param name="AlmostNoText">True when hardly anything could be read.</param>
/// <param name="Format">The file's real format.</param>
/// <param name="HasCameraData">True for a photo with camera EXIF data.</param>
/// <param name="HighThreshold">The proposed category's High threshold.</param>
/// <param name="MediumThreshold">The proposed category's Medium threshold.</param>
/// <param name="RulesOnly">True when only the keyword rules classified it (the hosted AI was not used).</param>
public sealed record ConfidenceInput(
    ConfidenceSignals Signals,
    bool AlmostNoText,
    FileFormat Format,
    bool HasCameraData,
    double HighThreshold,
    double MediumThreshold,
    bool RulesOnly);
