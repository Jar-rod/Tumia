using Tumiia.Domain.Projects;

namespace Tumiia.Application.Projects;

/// <summary>Creates a project vault, with the caller as its owner.</summary>
public sealed class CreateProject(IProjectStore projects, TimeProvider clock)
{
    /// <summary>Creates the project and returns it with the Owner role.</summary>
    public async Task<ProjectSummary> ExecuteAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var project = Project.Create(name, userId, clock.GetUtcNow());
        var owner = ProjectMember.Owner(project);
        await projects.AddAsync(project, owner, cancellationToken);
        return new ProjectSummary(project, owner.Role);
    }
}
