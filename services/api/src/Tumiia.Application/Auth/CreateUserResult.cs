namespace Tumiia.Application.Auth;

/// <summary>The outcome of creating an account.</summary>
/// <param name="User">The new account, when it was created.</param>
/// <param name="DuplicateEmail">True when an account with this email already exists.</param>
/// <param name="Errors">What was wrong with the input, by field ("email", "password").</param>
public sealed record CreateUserResult(UserAccount? User, bool DuplicateEmail, IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>The account was created.</summary>
    public static CreateUserResult Created(UserAccount user) => new(user, false, new Dictionary<string, string[]>());
}
