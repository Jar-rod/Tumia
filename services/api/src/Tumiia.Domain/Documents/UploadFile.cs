namespace Tumiia.Domain.Documents;

/// <summary>One file the browser wants to upload, as described before the upload starts.</summary>
public sealed record UploadFile(string FileName, string ContentType, long SizeBytes, string Sha256)
{
    /// <summary>The extension in lower case, with its dot (".pdf"), or an empty string.</summary>
    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();
}
