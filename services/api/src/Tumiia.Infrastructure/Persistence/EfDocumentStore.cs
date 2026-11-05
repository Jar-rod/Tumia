using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tumiia.Application.Documents;
using Tumiia.Domain.Documents;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>Documents in the <c>documents</c> table.</summary>
public sealed class EfDocumentStore(TumiiaDbContext database) : IDocumentStore
{
    private const string UniqueViolation = "23505";

    /// <inheritdoc />
    public Task<Document?> FindAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return database.Documents.SingleOrDefaultAsync(document => document.Id == documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Document?> FindByHashAsync(Guid projectId, string sha256, CancellationToken cancellationToken)
    {
        return database.Documents.SingleOrDefaultAsync(document => document.ProjectId == projectId && document.Sha256 == sha256, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The unique index on (project, hash) is what catches two requests racing with the same file.</remarks>
    public async Task<Document> AddOrGetExistingAsync(Document document, CancellationToken cancellationToken)
    {
        database.Documents.Add(document);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return document;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            database.Entry(document).State = EntityState.Detached;
            return await FindByHashAsync(document.ProjectId, document.Sha256, cancellationToken)
                ?? throw new InvalidOperationException("A duplicate was reported but could not be found.", exception);
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(Document document, CancellationToken cancellationToken)
    {
        return database.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Files whose upload never finished are left out, unless asked for by status.</remarks>
    public async Task<IReadOnlyList<Document>> ListAsync(Guid projectId, DocumentStatus? status, string? category, CancellationToken cancellationToken)
    {
        var query = database.Documents.Where(document => document.ProjectId == projectId);
        query = status is null
            ? query.Where(document => document.Status != DocumentStatus.PendingUpload)
            : query.Where(document => document.Status == status);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(document => document.DocumentTypeConfirmed == category);
        }

        return await query
            .OrderByDescending(document => document.UploadTimestamp)
            .ThenBy(document => document.OriginalFilename)
            .ToListAsync(cancellationToken);
    }
}
