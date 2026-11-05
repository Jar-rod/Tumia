namespace Tumiia.Application.Projects;

/// <summary>Lists the projects the caller belongs to.</summary>
public sealed class ListMyProjects(IProjectStore projects)
{
    /// <summary>The caller's projects, newest first.</summary>
    public Task<IReadOnlyList<ProjectSummary>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        return projects.ListForMemberAsync(userId, cancellationToken);
    }
}
