namespace Tumiia.Application.Projects;

/// <summary>Gets one project, if the caller belongs to it.</summary>
public sealed class GetProject(IProjectStore projects)
{
    /// <summary>The project, or null when it does not exist or the caller is not a member.</summary>
    public Task<ProjectSummary?> ExecuteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return projects.FindForMemberAsync(projectId, userId, cancellationToken);
    }
}
