using SkiaSharp;

namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>
/// Turns a photo or scan into a JPEG no larger than the model can use, so big phone photos fit in a request. Uses
/// SkiaSharp, which reads JPEG, PNG and WebP. TIFF and HEIC are not decoded, so those images are not sent.
/// </summary>
public static class ImageShrinker
{
    private const int JpegQuality = 85;

    /// <summary>The image as JPEG with its longest side at most <paramref name="maxEdge"/> pixels, or null when it cannot be decoded.</summary>
    public static byte[]? ToJpeg(byte[] image, int maxEdge)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(image));
        using var bitmap = codec is null ? null : SKBitmap.Decode(codec);
        if (bitmap is null)
        {
            return null;
        }

        var scale = Math.Min(1.0, maxEdge / (double)Math.Max(bitmap.Width, bitmap.Height));
        var size = new SKSizeI(Math.Max(1, (int)(bitmap.Width * scale)), Math.Max(1, (int)(bitmap.Height * scale)));
        using var resized = scale < 1.0 ? bitmap.Resize(size, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)) : null;
        using var data = (resized ?? bitmap).Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data?.ToArray();
    }
}
