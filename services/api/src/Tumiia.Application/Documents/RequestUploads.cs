using Tumiia.Application.Auth;
using Tumiia.Application.Projects;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Storage;

namespace Tumiia.Application.Documents;

/// <summary>
/// Step 1 of an upload: checks the files the browser wants to send, and gives back a pre-signed upload address
/// for each new file, or the existing document for a file already in the project (same SHA-256).
/// </summary>
public sealed class RequestUploads(
    ProjectAccessGuard access,
    IDocumentStore documents,
    IFileStore files,
    IAuditLog auditLog,
    UploadRules rules)
{
    /// <summary>How long an upload address works. Five minutes.</summary>
    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Checks every file first; if any is refused, nothing is created and the reasons are returned. Otherwise
    /// returns one <see cref="UploadTarget"/> per file, in the order sent.
    /// </summary>
    public async Task<RequestUploadsResult> ExecuteAsync(Guid userId, Guid projectId, IReadOnlyList<UploadFile> requested, CaptureSource captureSource, string ipAddress, CancellationToken cancellationToken)
    {
        if (!await access.CanAccessProjectAsync(userId, projectId, cancellationToken))
        {
            return new RequestUploadsResult(false, new Dictionary<string, string[]>(), []);
        }

        var errors = Check(requested);
        if (errors.Count > 0)
        {
            return new RequestUploadsResult(true, errors, []);
        }

        var targets = new List<UploadTarget>();
        foreach (var file in requested)
        {
            targets.Add(await PrepareAsync(userId, projectId, file, captureSource, ipAddress, cancellationToken));
        }

        return new RequestUploadsResult(true, errors, targets);
    }

    private Dictionary<string, string[]> Check(IReadOnlyList<UploadFile> requested)
    {
        var errors = new Dictionary<string, string[]>();
        var batchProblem = rules.CheckBatchSize(requested.Count);
        if (batchProblem is not null)
        {
            errors["files"] = [batchProblem];
            return errors;
        }

        for (var index = 0; index < requested.Count; index++)
        {
            var problem = rules.Check(requested[index]);
            if (problem is not null)
            {
                errors[$"files[{index}]"] = [$"{requested[index].FileName}: {problem}"];
            }
        }

        return errors;
    }

    /// <summary>
    /// A file already in the project is a duplicate: its document is returned instead. The one exception is an
    /// earlier upload of the same file that never finished; that document gets a fresh upload address.
    /// </summary>
    private async Task<UploadTarget> PrepareAsync(Guid userId, Guid projectId, UploadFile file, CaptureSource captureSource, string ipAddress, CancellationToken cancellationToken)
    {
        var existing = await documents.FindByHashAsync(projectId, file.Sha256.ToLowerInvariant(), cancellationToken);
        if (existing is { IsUploaded: true })
        {
            return new UploadTarget(file.FileName, null, null, existing.Id);
        }

        var document = existing ?? await documents.AddOrGetExistingAsync(Document.AwaitUpload(projectId, file, captureSource, userId), cancellationToken);
        if (document.IsUploaded)
        {
            return new UploadTarget(file.FileName, null, null, document.Id);
        }

        var url = await files.CreateUploadUrlAsync(document.StorageKey, document.ContentTypeClaimed, document.SizeBytes, UploadUrlLifetime, cancellationToken);
        await auditLog.RecordAsync(AuditEventType.DocumentUploadRequested, userId, ipAddress, true, cancellationToken, document.Id);
        return new UploadTarget(file.FileName, document.Id, url, null);
    }
}
