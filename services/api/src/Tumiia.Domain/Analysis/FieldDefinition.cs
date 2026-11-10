namespace Tumiia.Domain.Analysis;

/// <summary>
/// A field a category wants saved when a person confirms a document, for example a payslip's <c>gross_pay</c>.
/// <see cref="Labels"/> are the words that come before the value in a document ("Gross pay", "Total earnings"),
/// so fields can be added through configuration alone.
/// </summary>
public sealed record FieldDefinition
{
    /// <summary>The field's name, in snake_case.</summary>
    public required string Name { get; init; }

    /// <summary>What kind of value it is.</summary>
    public FieldType Type { get; init; } = FieldType.Text;

    /// <summary>Whether the category expects every such document to have it.</summary>
    public bool Required { get; init; }

    /// <summary>Words that come before the value in a document. Leave empty for the standard fields TUMIIA always looks for.</summary>
    public IReadOnlyList<string> Labels { get; init; } = [];
}
