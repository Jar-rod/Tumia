namespace Tumiia.Domain.Storage;

/// <summary>
/// Where original files live (Amazon S3). File bytes never pass through the API: browsers upload and download
/// directly with short-lived pre-signed addresses.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// An address the browser can PUT exactly this file to. The size and content type are signed into it, so
    /// storage rejects an upload of any other size or type.
    /// </summary>
    Task<string> CreateUploadUrlAsync(string storageKey, string contentType, long sizeBytes, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>An address the browser can GET the file from, shown in the page (inline) or saved (attachment) under its original name.</summary>
    Task<string> CreateDownloadUrlAsync(string storageKey, string fileName, bool inline, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>The stored file's size in bytes, or null when there is no file at that key.</summary>
    Task<long?> GetSizeAsync(string storageKey, CancellationToken cancellationToken);
}
