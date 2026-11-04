namespace Tumiia.Application.Auth;

/// <summary>Looks up the signed-in user, for <c>GET /auth/me</c>.</summary>
public sealed class GetCurrentUser(IUserAccounts userAccounts)
{
    /// <summary>Returns the user, or null if the account no longer exists.</summary>
    public Task<UserAccount?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        return userAccounts.FindByIdAsync(userId, cancellationToken);
    }
}
