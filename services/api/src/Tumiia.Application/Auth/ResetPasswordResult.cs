namespace Tumiia.Application.Auth;

/// <summary>The outcome of resetting a password.</summary>
/// <param name="Succeeded">True when the password was changed.</param>
/// <param name="InvalidLink">True when the email or reset token was wrong or expired. The cause is not revealed.</param>
/// <param name="PasswordErrors">Why the new password was rejected (too short, ...), when it was.</param>
public sealed record ResetPasswordResult(bool Succeeded, bool InvalidLink, IReadOnlyList<string> PasswordErrors)
{
    /// <summary>The password was changed.</summary>
    public static ResetPasswordResult Done { get; } = new(true, false, []);

    /// <summary>The link was wrong or expired.</summary>
    public static ResetPasswordResult BadLink { get; } = new(false, true, []);

    /// <summary>The link was fine but the new password broke the policy, for these reasons.</summary>
    public static ResetPasswordResult WeakPassword(IReadOnlyList<string> errors) => new(false, false, errors);
}
