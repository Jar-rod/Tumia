using Microsoft.AspNetCore.Identity;

namespace Tumiia.Infrastructure.Identity;

/// <summary>A user account as ASP.NET Core Identity stores it, with a Guid id and a display name.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>The name shown in the app and in emails.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
