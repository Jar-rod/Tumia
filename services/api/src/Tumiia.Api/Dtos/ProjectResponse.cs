using Tumiia.Domain.Projects;

namespace Tumiia.Api.Dtos;

/// <summary>A project vault.</summary>
/// <param name="Id">The project id.</param>
/// <param name="Name">The project's name.</param>
/// <param name="CreatedAt">When it was created (UTC).</param>
/// <param name="Role">The caller's role in the project.</param>
public sealed record ProjectResponse(Guid Id, string Name, DateTimeOffset CreatedAt, ProjectRole Role);
