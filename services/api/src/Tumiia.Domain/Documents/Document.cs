namespace Tumiia.Domain.Documents;

/// <summary>
/// One uploaded file and what is known about it (the v1.1 section 7 record). The original file in S3 is never
/// changed; confirmed values are stored here, separately from any AI prediction.
/// </summary>
public sealed class Document
{
    private Document()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    /// <summary>Where the original lives in S3: <c>originals/{projectId}/{documentId}</c>.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public string OriginalFilename { get; private set; } = string.Empty;

    /// <summary>The content type the browser said the file has. Not trusted; the analysis detects the real one.</summary>
    public string ContentTypeClaimed { get; private set; } = string.Empty;

    /// <summary>The content type found by reading the file (set by the analysis).</summary>
    public string? ContentTypeDetected { get; private set; }

    /// <summary>Whether the real format matches the file extension (set by the analysis).</summary>
    public bool? ExtensionMatches { get; private set; }

    public long SizeBytes { get; private set; }

    /// <summary>Hex SHA-256 of the file, as worked out by the browser. Used to spot duplicates in a project.</summary>
    public string Sha256 { get; private set; } = string.Empty;

    public CaptureSource CaptureSource { get; private set; }

    public Guid UploadedBy { get; private set; }

    /// <summary>When the upload finished. Null while <see cref="DocumentStatus.PendingUpload"/>.</summary>
    public DateTimeOffset? UploadTimestamp { get; private set; }

    public DocumentStatus Status { get; private set; }

    public ConsequenceLevel? ConsequenceLevel { get; private set; }

    public bool NeedsReview { get; private set; }

    /// <summary>True once a person changed something the AI proposed.</summary>
    public bool CorrectionFlag { get; private set; }

    public Guid? ConfirmedBy { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public string? DocumentTypeConfirmed { get; private set; }

    public string? SubjectConfirmed { get; private set; }

    /// <summary>The fields a person confirmed, as JSON. Null until confirmation.</summary>
    public string? ConfirmedFields { get; private set; }

    /// <summary>A new document waiting for its file. The id is chosen now, so the S3 key is known before the upload.</summary>
    public static Document AwaitUpload(Guid projectId, UploadFile file, CaptureSource captureSource, Guid uploadedBy)
    {
        var id = Guid.NewGuid();
        return new Document
        {
            Id = id,
            ProjectId = projectId,
            StorageKey = $"originals/{projectId}/{id}",
            OriginalFilename = file.FileName,
            ContentTypeClaimed = file.ContentType,
            SizeBytes = file.SizeBytes,
            Sha256 = file.Sha256.ToLowerInvariant(),
            CaptureSource = captureSource,
            UploadedBy = uploadedBy,
            Status = DocumentStatus.PendingUpload,
        };
    }

    /// <summary>Records that the file is in S3, so it can be analysed. Only a document still waiting for its file can be marked.</summary>
    public void MarkUploaded(DateTimeOffset now)
    {
        if (Status != DocumentStatus.PendingUpload)
        {
            throw new InvalidOperationException($"Document {Id} is {Status}, not waiting for an upload.");
        }

        Status = DocumentStatus.New;
        UploadTimestamp = now;
    }

    /// <summary>True once the file has arrived in storage.</summary>
    public bool IsUploaded => Status != DocumentStatus.PendingUpload;
}
