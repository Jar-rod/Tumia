using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tumiia.Application.Auth;
using Tumiia.Application.Projects;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Storage;

namespace Tumiia.Application.Documents;

/// <summary>
/// Takes text or a message the user pasted in (August brief: "drags, uploads or pastes"). The text is stored
/// unchanged as a .txt original, exactly like an uploaded file, and queued for the same analysis. Pasting the same
/// text into the same project twice gives back the first document.
/// </summary>
public sealed class CapturePastedText(
    ProjectAccessGuard access,
    IDocumentStore documents,
    IFileStore files,
    IDocumentQueue queue,
    IAuditLog auditLog,
    UploadRules rules,
    TimeProvider clock)
{
    /// <summary>The content type the pasted text is stored with.</summary>
    public const string ContentType = "text/plain";

    private const int MaxTitleLength = 100;

    /// <summary>Stores and queues the pasted text. <paramref name="title"/> becomes the file name, if given.</summary>
    public async Task<CapturePastedTextResult> ExecuteAsync(Guid userId, Guid projectId, string text, string? title, string ipAddress, CancellationToken cancellationToken)
    {
        if (!await access.CanAccessProjectAsync(userId, projectId, cancellationToken))
        {
            return new CapturePastedTextResult(false, null, null, false);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new CapturePastedTextResult(true, "Paste some text first.", null, false);
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        var file = new UploadFile(FileNameFor(title), ContentType, bytes.Length, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        var problem = rules.Check(file);
        if (problem is not null)
        {
            return new CapturePastedTextResult(true, problem, null, false);
        }

        var existing = await documents.FindByHashAsync(projectId, file.Sha256, cancellationToken);
        var document = existing ?? await documents.AddOrGetExistingAsync(Document.AwaitUpload(projectId, file, CaptureSource.Paste, userId), cancellationToken);
        if (document.IsUploaded)
        {
            return new CapturePastedTextResult(true, null, document, true);
        }

        await files.UploadAsync(document.StorageKey, bytes, ContentType, cancellationToken);
        document.MarkUploaded(clock.GetUtcNow());
        await documents.SaveAsync(document, cancellationToken);
        await auditLog.RecordAsync(AuditEventType.DocumentUploaded, userId, ipAddress, true, cancellationToken, document.Id);
        await queue.EnqueueForAnalysisAsync(document.Id, cancellationToken);
        return new CapturePastedTextResult(true, null, document, false);
    }

    /// <summary>"Site instruction.txt" from the title, or "Pasted text 2026-11-10 0930.txt" without one. Characters not allowed in file names are dropped.</summary>
    public string FileNameFor(string? title)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        var cleaned = new string((title ?? string.Empty).Where(letter => !invalid.Contains(letter) && !char.IsControl(letter)).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = "Pasted text " + clock.GetUtcNow().ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture);
        }

        return (cleaned.Length > MaxTitleLength ? cleaned[..MaxTitleLength] : cleaned) + ".txt";
    }
}
