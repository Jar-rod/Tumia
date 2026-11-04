namespace Tumiia.Domain.Auth;

/// <summary>
/// One refresh token, as stored in the database. Only a SHA-256 hash of the token is kept, never the token itself,
/// so a database leak cannot be used to sign in. Tokens are rotated: each use replaces the token with a new one in
/// the same family. If a token that was already replaced is presented again, someone may have stolen it, and the
/// whole family is revoked. A token ended for another reason (logout, password reset) is simply no longer active.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>All tokens descended from one sign-in share a family id, so one sign-in can be ended as a whole.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Hex SHA-256 of the token value.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When the token stopped being usable (replaced, logged out, or revoked for safety). Null while active.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The token that replaced this one when it was used, if any.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>Starts a new family: used when a user registers or logs in.</summary>
    public static RefreshToken StartFamily(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        return Create(userId, Guid.NewGuid(), tokenHash, now, lifetime);
    }

    /// <summary>
    /// Uses this token: revokes it and returns its replacement in the same family. Callers must check
    /// <see cref="IsActive"/> first.
    /// </summary>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (!IsActive(now))
        {
            throw new InvalidOperationException("Only an active refresh token can be rotated.");
        }

        var replacement = Create(UserId, FamilyId, newTokenHash, now, lifetime);
        RevokedAt = now;
        ReplacedByTokenId = replacement.Id;
        return replacement;
    }

    /// <summary>True while the token has not been revoked and has not expired.</summary>
    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && now < ExpiresAt;
    }

    /// <summary>
    /// True when the token was already swapped for a new one. Presenting it again means it was used twice, which a
    /// real browser never does, so the whole family must be ended.
    /// </summary>
    public bool IsReuse()
    {
        return ReplacedByTokenId is not null;
    }

    /// <summary>Stops the token working. Does nothing if it was already revoked, so the first reason is kept.</summary>
    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }

    private static RefreshToken Create(Guid userId, Guid familyId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }
}
