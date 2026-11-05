using Tumiia.Domain.Projects;

namespace Tumiia.Application.Projects;

/// <summary>Projects and their members. Implemented with EF Core in Infrastructure.</summary>
public interface IProjectStore
{
    /// <summary>Saves a new project and its owner membership together.</summary>
    Task AddAsync(Project project, ProjectMember owner, CancellationToken cancellationToken);

    /// <summary>The projects the user is a member of, newest first.</summary>
    Task<IReadOnlyList<ProjectSummary>> ListForMemberAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The project, if the user is a member of it; otherwise null (also when it does not exist).</summary>
    Task<ProjectSummary?> FindForMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
}
