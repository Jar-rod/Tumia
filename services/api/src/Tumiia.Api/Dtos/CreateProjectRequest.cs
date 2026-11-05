using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>A new project vault.</summary>
public sealed record CreateProjectRequest
{
    /// <summary>The project's name, for example "Mokoena Residence".</summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }
}
