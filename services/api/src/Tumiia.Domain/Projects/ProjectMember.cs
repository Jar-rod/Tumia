namespace Tumiia.Domain.Projects;

/// <summary>Says a user belongs to a project, and in what role. Every access check goes through this.</summary>
public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public ProjectRole Role { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    /// <summary>Makes the project's creator its owner.</summary>
    public static ProjectMember Owner(Project project)
    {
        return new ProjectMember { ProjectId = project.Id, UserId = project.OwnerUserId, Role = ProjectRole.Owner, AddedAt = project.CreatedAt };
    }
}
