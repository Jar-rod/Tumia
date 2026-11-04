namespace Tumiia.Application.Auth;

/// <summary>The outcome of checking a password, including what it did to the lockout counter.</summary>
public enum PasswordCheckResult
{
    /// <summary>Right password; the failed-attempt counter was reset.</summary>
    Succeeded,

    /// <summary>Wrong password; one more failed attempt was counted.</summary>
    WrongPassword,

    /// <summary>Wrong password, and that attempt locked the account.</summary>
    JustLockedOut,

    /// <summary>The account was already locked, so the password was not checked.</summary>
    LockedOut,
}
