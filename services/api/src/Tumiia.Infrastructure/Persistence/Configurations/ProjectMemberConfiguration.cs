using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tumiia.Domain.Projects;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>The <c>project_members</c> table: one row per user per project, with the role stored as text.</summary>
public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    /// <summary>Maps the membership to its table.</summary>
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");
        builder.HasKey(member => new { member.ProjectId, member.UserId });
        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(member => member.UserId);
        builder.HasOne<Project>().WithMany().HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
