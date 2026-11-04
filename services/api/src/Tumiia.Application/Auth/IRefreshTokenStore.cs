using Tumiia.Domain.Auth;

namespace Tumiia.Application.Auth;

/// <summary>Stores refresh tokens (hashes only). Implemented with EF Core in Infrastructure.</summary>
public interface IRefreshTokenStore
{
    /// <summary>Saves a new token.</summary>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    /// <summary>Finds a token by the hash of its value, or null.</summary>
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Saves a rotation: the old token (now revoked) and its replacement, together.</summary>
    Task SaveRotationAsync(RefreshToken oldToken, RefreshToken replacement, CancellationToken cancellationToken);

    /// <summary>Revokes every still-active token in a family (one sign-in).</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes every still-active token of a user (all their sign-ins).</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
