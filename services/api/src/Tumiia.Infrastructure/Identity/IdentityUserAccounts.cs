using Microsoft.AspNetCore.Identity;
using Tumiia.Application.Auth;
using Tumiia.Domain.Auth;

namespace Tumiia.Infrastructure.Identity;

/// <summary>User accounts backed by ASP.NET Core Identity (passwords hashed with PBKDF2, lockout, reset tokens).</summary>
public sealed class IdentityUserAccounts(UserManager<ApplicationUser> userManager) : IUserAccounts
{
    private const string InvalidTokenErrorCode = "InvalidToken";

    /// <inheritdoc />
    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await ToAccountAsync(user);
    }

    /// <inheritdoc />
    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToAccountAsync(user);
    }

    /// <inheritdoc />
    public async Task<CreateUserResult> CreateAsync(string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return ToFailure(result.Errors);
        }

        await userManager.AddToRoleAsync(user, Roles.User);
        return CreateUserResult.Created(await ToAccountAsync(user));
    }

    /// <inheritdoc />
    public async Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return PasswordCheckResult.WrongPassword;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return PasswordCheckResult.LockedOut;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return PasswordCheckResult.Succeeded;
        }

        await userManager.AccessFailedAsync(user);
        return await userManager.IsLockedOutAsync(user) ? PasswordCheckResult.JustLockedOut : PasswordCheckResult.WrongPassword;
    }

    /// <inheritdoc />
    public async Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null && !await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    /// <inheritdoc />
    public async Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} does not exist.");
        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string resetToken, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ResetPasswordResult.BadLink;
        }

        var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (result.Errors.Any(error => error.Code == InvalidTokenErrorCode))
        {
            return ResetPasswordResult.BadLink;
        }

        if (!result.Succeeded)
        {
            return ResetPasswordResult.WeakPassword(result.Errors.Select(error => error.Description).ToList());
        }

        // Proving control of the email is enough to lift a lockout.
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return ResetPasswordResult.Done;
    }

    private async Task<UserAccount> ToAccountAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserAccount(user.Id, user.Email!, user.DisplayName, roles.ToList());
    }

    /// <summary>
    /// Turns Identity's errors into errors per field. A taken email is reported separately so the API can answer 409.
    /// </summary>
    private static CreateUserResult ToFailure(IEnumerable<IdentityError> errors)
    {
        var errorList = errors.ToList();
        var duplicate = errorList.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");
        var byField = errorList
            .Where(error => error.Code is not ("DuplicateEmail" or "DuplicateUserName"))
            .GroupBy(error => error.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email")
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

        return new CreateUserResult(null, duplicate, byField);
    }
}
