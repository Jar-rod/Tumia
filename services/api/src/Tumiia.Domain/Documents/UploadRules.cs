namespace Tumiia.Domain.Documents;

/// <summary>
/// What may be uploaded: which file extensions, how big, and how many files at once. The values come from
/// configuration (<c>Uploads</c> section), so they can change without a code change.
/// </summary>
public sealed class UploadRules
{
    /// <summary>The extensions TUMIIA accepts, with their dots, in lower case.</summary>
    public required IReadOnlySet<string> AllowedExtensions { get; init; }

    /// <summary>The largest file accepted, in megabytes. 50 by default.</summary>
    public int MaxFileSizeMb { get; init; } = 50;

    /// <summary>The most files in one upload request. 50 by default.</summary>
    public int MaxFilesPerRequest { get; init; } = 50;

    /// <summary>The largest file accepted, in bytes.</summary>
    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;

    /// <summary>Returns why one file cannot be uploaded, in plain language, or null when it can.</summary>
    public string? Check(UploadFile file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName) || file.FileName.Length > 255)
        {
            return "The file name must be 1 to 255 characters.";
        }

        if (!AllowedExtensions.Contains(file.Extension))
        {
            var shown = file.Extension.Length == 0 ? "files without an extension" : $"{file.Extension} files";
            return $"TUMIIA can't take {shown}. Allowed: {string.Join(" ", AllowedExtensions.Order())}.";
        }

        if (file.SizeBytes <= 0)
        {
            return "The file is empty.";
        }

        if (file.SizeBytes > MaxFileSizeBytes)
        {
            return $"The file is larger than the {MaxFileSizeMb} MB limit.";
        }

        return IsSha256(file.Sha256) ? null : "The SHA-256 hash must be 64 hexadecimal characters.";
    }

    /// <summary>Returns why a batch is too big, or null when it is fine.</summary>
    public string? CheckBatchSize(int fileCount)
    {
        if (fileCount < 1)
        {
            return "Send at least one file.";
        }

        return fileCount > MaxFilesPerRequest ? $"Send at most {MaxFilesPerRequest} files at a time." : null;
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64 && value.All(char.IsAsciiHexDigit);
    }
}
