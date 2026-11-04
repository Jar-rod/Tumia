using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tumiia.Domain.Auth;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>
/// Gives Identity's tables plain names (<c>users</c> rather than <c>asp_net_users</c>) and seeds the two roles.
/// The role ids are fixed so the seed data never changes between migrations.
/// </summary>
public static class IdentityTables
{
    public static readonly Guid UserRoleId = new("6a3b1f0e-2c4d-4e5f-8a9b-0c1d2e3f4a5b");

    public static readonly Guid AdminRoleId = new("9e8d7c6b-5a4f-4e3d-2c1b-0a9f8e7d6c5b");

    /// <summary>Renames the Identity tables and adds the User and Admin roles.</summary>
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("users");
            user.Property(account => account.DisplayName).HasMaxLength(100).IsRequired();
        });
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<IdentityRole<Guid>>().HasData(
            Role(UserRoleId, Roles.User),
            Role(AdminRoleId, Roles.Admin));
    }

    private static IdentityRole<Guid> Role(Guid id, string name)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = id.ToString(),
        };
    }
}
