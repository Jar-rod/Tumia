using Microsoft.AspNetCore.Identity;

namespace Tumiia.Infrastructure.Identity;

/// <summary>The password and lockout rules, in one place so they can be tested.</summary>
public static class IdentityRules
{
    /// <summary>
    /// Passwords: at least 10 characters plus Identity's defaults (a digit, a lower-case and an upper-case letter,
    /// and a symbol). Lockout: 5 wrong passwords lock the account for 15 minutes. The email is the user name, so
    /// the user-name character check is off (the email check still runs).
    /// </summary>
    public static void Configure(IdentityOptions options)
    {
        options.Password.RequiredLength = 10;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
    }
}
