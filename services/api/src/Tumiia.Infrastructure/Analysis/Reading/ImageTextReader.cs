using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>
/// Reads what a photo, scan or screenshot says about itself: its EXIF data (when and where it was taken, which
/// camera). The words in the image are read by the hosted AI, not here.
/// </summary>
public static class ImageTextReader
{
    /// <summary>No text, and the photo's EXIF facts if it has any.</summary>
    public static DocumentText Read(byte[] content) => new() { Image = ReadFacts(content) };

    /// <summary>The EXIF facts, or null when the image has none (screenshots usually have none).</summary>
    public static ImageFacts? ReadFacts(byte[] content)
    {
        IReadOnlyList<MetadataExtractor.Directory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(new MemoryStream(content));
        }
        catch (Exception exception) when (exception is ImageProcessingException or IOException)
        {
            return null;
        }

        var takenAt = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault() is { } exif && exif.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var date)
            ? new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc))
            : (DateTimeOffset?)null;
        var location = directories.OfType<GpsDirectory>().FirstOrDefault()?.GetGeoLocation();
        var make = directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDescription(ExifDirectoryBase.TagMake);

        var facts = new ImageFacts(takenAt, location?.Latitude, location?.Longitude, string.IsNullOrWhiteSpace(make) ? null : make.Trim());
        return facts is { TakenAt: null, Latitude: null, CameraMake: null } ? null : facts;
    }
}
