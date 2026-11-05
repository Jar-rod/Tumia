namespace Tumiia.Domain.Projects;

/// <summary>A project vault: one building project, holding its documents. Belongs to the user who created it.</summary>
public sealed class Project
{
    /// <summary>The longest name allowed.</summary>
    public const int MaxNameLength = 100;

    private Project()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid OwnerUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a project. The name is trimmed and must not be empty or longer than 100 characters.</summary>
    public static Project Create(string name, Guid ownerUserId, DateTimeOffset now)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"A project name must be 1 to {MaxNameLength} characters.", nameof(name));
        }

        return new Project { Id = Guid.NewGuid(), Name = trimmed, OwnerUserId = ownerUserId, CreatedAt = now };
    }
}
