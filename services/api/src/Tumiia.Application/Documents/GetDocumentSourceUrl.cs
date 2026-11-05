using Tumiia.Application.Projects;
using Tumiia.Domain.Storage;

namespace Tumiia.Application.Documents;

/// <summary>Gives a short-lived address to view or download a document's original file.</summary>
public sealed class GetDocumentSourceUrl(ProjectAccessGuard access, IFileStore files, TimeProvider clock)
{
    /// <summary>How long the address works. Five minutes.</summary>
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Formats a browser can show itself. Anything else downloads as a file.</summary>
    public static readonly IReadOnlySet<string> ShownInBrowser = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp",
    };

    /// <summary>The address, or why there is none (see <see cref="SourceUrlResult"/>).</summary>
    public async Task<SourceUrlResult> ExecuteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await access.FindAccessibleDocumentAsync(userId, documentId, cancellationToken);
        if (document is null)
        {
            return new SourceUrlResult(false, null, null);
        }

        if (!document.IsUploaded)
        {
            return new SourceUrlResult(true, null, null);
        }

        var inline = ShownInBrowser.Contains(Path.GetExtension(document.OriginalFilename));
        var url = await files.CreateDownloadUrlAsync(document.StorageKey, document.OriginalFilename, inline, DownloadUrlLifetime, cancellationToken);
        return new SourceUrlResult(true, url, clock.GetUtcNow().Add(DownloadUrlLifetime));
    }
}
