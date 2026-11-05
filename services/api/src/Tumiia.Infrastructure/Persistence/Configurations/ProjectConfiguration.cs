using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tumiia.Domain.Projects;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.Infrastructure.Persistence.Configurations;

/// <summary>The <c>projects</c> table. A project cannot be deleted while it has documents (see the documents table).</summary>
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <summary>Maps the project to its table.</summary>
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Name).HasMaxLength(Project.MaxNameLength).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(project => project.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
