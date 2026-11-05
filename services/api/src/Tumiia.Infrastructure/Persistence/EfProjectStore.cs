using Microsoft.EntityFrameworkCore;
using Tumiia.Application.Projects;
using Tumiia.Domain.Projects;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>Projects in the <c>projects</c> table, and memberships in <c>project_members</c>.</summary>
public sealed class EfProjectStore(TumiiaDbContext database) : IProjectStore
{
    /// <inheritdoc />
    public async Task AddAsync(Project project, ProjectMember owner, CancellationToken cancellationToken)
    {
        database.Projects.Add(project);
        database.ProjectMembers.Add(owner);
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectSummary>> ListForMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        var query = from member in database.ProjectMembers
                    join project in database.Projects on member.ProjectId equals project.Id
                    where member.UserId == userId
                    orderby project.CreatedAt descending
                    select new ProjectSummary(project, member.Role);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Filters before building the summary, because EF Core cannot filter on a summary's properties.</remarks>
    public Task<ProjectSummary?> FindForMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var query = from member in database.ProjectMembers
                    join project in database.Projects on member.ProjectId equals project.Id
                    where member.UserId == userId && project.Id == projectId
                    select new ProjectSummary(project, member.Role);

        return query.SingleOrDefaultAsync(cancellationToken);
    }
}
