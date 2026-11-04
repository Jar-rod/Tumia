namespace Tumiia.Application.Auth;

/// <summary>User accounts and passwords. Implemented with ASP.NET Core Identity in Infrastructure.</summary>
public interface IUserAccounts
{
    /// <summary>Finds an account by email (not case-sensitive), or null.</summary>
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Finds an account by id, or null.</summary>
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Creates an account in the User role. Checks the password policy and that the email is unused.</summary>
    Task<CreateUserResult> CreateAsync(string email, string password, string displayName, CancellationToken cancellationToken);

    /// <summary>Checks a password and updates the failed-attempt counter (5 failures lock the account for 15 minutes).</summary>
    Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    /// <summary>Adds the user to a role if they are not in it already.</summary>
    Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken);

    /// <summary>Makes a one-hour password reset token.</summary>
    Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Changes the password if the reset token is valid and the new password meets the policy.</summary>
    Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string resetToken, string newPassword, CancellationToken cancellationToken);
}
