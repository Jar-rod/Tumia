using Tumiia.Application.Auth;
using Tumiia.Application.Projects;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Storage;

namespace Tumiia.Application.Documents;

/// <summary>
/// Step 2 of an upload: the browser says the file is in storage. Checks it really is, and is the size announced,
/// then marks the document New and queues it for analysis. Safe to call again: a retry re-queues a document that
/// is still New (the analysis ignores repeats) and does nothing once analysis has started.
/// </summary>
public sealed class ConfirmUpload(
    ProjectAccessGuard access,
    IDocumentStore documents,
    IFileStore files,
    IDocumentQueue queue,
    IAuditLog auditLog,
    TimeProvider clock)
{
    /// <summary>Confirms the upload; see <see cref="ConfirmUploadOutcome"/> for the possible results.</summary>
    public async Task<ConfirmUploadResult> ExecuteAsync(Guid userId, Guid documentId, string ipAddress, CancellationToken cancellationToken)
    {
        var document = await access.FindAccessibleDocumentAsync(userId, documentId, cancellationToken);
        if (document is null)
        {
            return new ConfirmUploadResult(ConfirmUploadOutcome.NotFound, null);
        }

        if (document.Status == DocumentStatus.PendingUpload)
        {
            var problem = await CheckStoredFileAsync(document, cancellationToken);
            if (problem is not null)
            {
                return new ConfirmUploadResult(problem.Value, document);
            }

            document.MarkUploaded(clock.GetUtcNow());
            await documents.SaveAsync(document, cancellationToken);
            await auditLog.RecordAsync(AuditEventType.DocumentUploaded, userId, ipAddress, true, cancellationToken, document.Id);
        }

        if (document.Status == DocumentStatus.New)
        {
            await queue.EnqueueForAnalysisAsync(document.Id, cancellationToken);
        }

        return new ConfirmUploadResult(ConfirmUploadOutcome.Queued, document);
    }

    private async Task<ConfirmUploadOutcome?> CheckStoredFileAsync(Document document, CancellationToken cancellationToken)
    {
        var storedSize = await files.GetSizeAsync(document.StorageKey, cancellationToken);
        if (storedSize is null)
        {
            return ConfirmUploadOutcome.FileMissing;
        }

        return storedSize == document.SizeBytes ? null : ConfirmUploadOutcome.SizeMismatch;
    }
}
