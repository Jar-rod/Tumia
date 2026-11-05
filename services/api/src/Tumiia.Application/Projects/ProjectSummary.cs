using Tumiia.Domain.Projects;

namespace Tumiia.Application.Projects;

/// <summary>A project together with the caller's role in it.</summary>
public sealed record ProjectSummary(Project Project, ProjectRole Role);
