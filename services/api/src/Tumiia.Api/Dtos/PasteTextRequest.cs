using System.ComponentModel.DataAnnotations;

namespace Tumiia.Api.Dtos;

/// <summary>Text or a message the user pasted in.</summary>
public sealed record PasteTextRequest
{
    /// <summary>The text, exactly as pasted. Stored unchanged as a .txt file.</summary>
    [Required(ErrorMessage = "Paste some text first.")]
    [MaxLength(200_000, ErrorMessage = "The pasted text is too long. Paste at most 200,000 characters.")]
    public required string Text { get; init; }

    /// <summary>An optional title, used as the file name ("Site instruction" becomes "Site instruction.txt").</summary>
    [MaxLength(100)]
    public string? Title { get; init; }
}
