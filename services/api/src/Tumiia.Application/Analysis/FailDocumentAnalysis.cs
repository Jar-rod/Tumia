using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Domain.Audit;

namespace Tumiia.Application.Analysis;

/// <summary>
/// Gives up on a document whose analysis kept failing (the queue's last try): marks it Failed with the reason, so a
/// person sees it in Needs Review instead of it waiting forever.
/// </summary>
public sealed class FailDocumentAnalysis(IDocumentStore documents, IAuditLog auditLog)
{
    /// <summary>Marks the document Failed, unless it has already left the analysis (then nothing changes).</summary>
    public async Task ExecuteAsync(Guid documentId, string reason, CancellationToken cancellationToken)
    {
        var document = await documents.FindAsync(documentId, cancellationToken);
        if (document is null || !document.IsAwaitingAnalysis)
        {
            return;
        }

        document.MarkAnalysisFailed(reason);
        await documents.SaveAsync(document, cancellationToken);
        await auditLog.RecordAsync(AuditEventType.DocumentAnalysisFailed, null, AnalysisPipeline.SystemIpAddress, false, cancellationToken, document.Id);
    }
}
