using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Tumiia.Domain.Storage;
using Tumiia.Infrastructure.Aws;

namespace Tumiia.Infrastructure.Storage;

/// <summary>Original files in Amazon S3, reached by the browser through pre-signed addresses.</summary>
public sealed class S3FileStore(IAmazonS3 s3, IOptions<StorageSettings> storage, IOptions<AwsSettings> aws, TimeProvider clock) : IFileStore
{
    /// <summary>
    /// A pre-signed PUT address. Content-Length and Content-Type are signed into it, so S3 rejects an upload of a
    /// different size or type (the browser must send exactly the size and type it announced).
    /// </summary>
    public async Task<string> CreateUploadUrlAsync(string storageKey, string contentType, long sizeBytes, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var request = NewRequest(storageKey, HttpVerb.PUT, lifetime);
        request.ContentType = contentType;
        request.Headers["Content-Length"] = sizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return await s3.GetPreSignedURLAsync(request);
    }

    /// <summary>
    /// A pre-signed GET address. S3 answers with a Content-Disposition header, so the browser shows the file (inline)
    /// or saves it (attachment) under its original name.
    /// </summary>
    public async Task<string> CreateDownloadUrlAsync(string storageKey, string fileName, bool inline, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var request = NewRequest(storageKey, HttpVerb.GET, lifetime);
        request.ResponseHeaderOverrides.ContentDisposition = ContentDisposition(fileName, inline);
        return await s3.GetPreSignedURLAsync(request);
    }

    /// <summary>The object's size from its metadata, or null when there is no such object.</summary>
    public async Task<long?> GetSizeAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await s3.GetObjectMetadataAsync(storage.Value.DocumentsBucket, storageKey, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the header value with both a plain <c>filename</c> and an encoded <c>filename*</c>, so names with
    /// spaces, quotes or non-English letters come out right in every browser.
    /// </summary>
    public static string ContentDisposition(string fileName, bool inline)
    {
        var header = new ContentDispositionHeaderValue(inline ? "inline" : "attachment");
        header.SetHttpFileName(fileName);
        return header.ToString();
    }

    // LocalStack runs on plain HTTP locally; real S3 is always HTTPS.
    private GetPreSignedUrlRequest NewRequest(string storageKey, HttpVerb verb, TimeSpan lifetime)
    {
        return new GetPreSignedUrlRequest
        {
            BucketName = storage.Value.DocumentsBucket,
            Key = storageKey,
            Verb = verb,
            Expires = clock.GetUtcNow().Add(lifetime).UtcDateTime,
            Protocol = aws.Value.ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTP : Protocol.HTTPS,
        };
    }
}
