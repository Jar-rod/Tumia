namespace Tumiia.Domain.Analysis;

/// <summary>What a photo's EXIF data says: when and where it was taken, and with which camera.</summary>
/// <param name="TakenAt">When the photo was taken, if recorded.</param>
/// <param name="Latitude">GPS latitude, if recorded.</param>
/// <param name="Longitude">GPS longitude, if recorded.</param>
/// <param name="CameraMake">The camera or phone maker, if recorded. A make means a real camera took it.</param>
public sealed record ImageFacts(DateTimeOffset? TakenAt, double? Latitude, double? Longitude, string? CameraMake)
{
    /// <summary>True when the photo came from a camera (not a screenshot or a scan).</summary>
    public bool HasCameraData => !string.IsNullOrWhiteSpace(CameraMake) || TakenAt is not null;
}
