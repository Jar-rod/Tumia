using Microsoft.EntityFrameworkCore;
using Tumiia.Application.Auth;
using Tumiia.Domain.Auth;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>Refresh tokens in the <c>refresh_tokens</c> table.</summary>
public sealed class EfRefreshTokenStore(TumiiaDbContext database) : IRefreshTokenStore
{
    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        database.RefreshTokens.Add(token);
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return database.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The old token was loaded by this context, so its change (revoked) is saved together with the new token.</remarks>
    public async Task SaveRotationAsync(RefreshToken oldToken, RefreshToken replacement, CancellationToken cancellationToken)
    {
        database.RefreshTokens.Update(oldToken);
        database.RefreshTokens.Add(replacement);
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return database.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return database.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }
}
