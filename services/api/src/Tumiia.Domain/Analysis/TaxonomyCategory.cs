using Tumiia.Domain.Documents;

namespace Tumiia.Domain.Analysis;

/// <summary>
/// One filing category (Quote, Payment, ...) and everything needed to recognise it: keywords, aliases and patterns
/// for the rules, a description for the model, the subject chips shown to the user, the fields it expects and saves,
/// how much a wrong filing matters, and its confidence thresholds. Stored in the database, so it can be changed
/// without a code change.
/// </summary>
public sealed record TaxonomyCategory
{
    /// <summary>The key used for the category that holds what cannot be classified.</summary>
    public const string NeedsReviewKey = "needs_review";

    /// <summary>A stable key, such as <c>commercial_quote</c>.</summary>
    public required string Key { get; init; }

    /// <summary>The name people see, such as "Quote".</summary>
    public required string DisplayName { get; init; }

    /// <summary>One plain sentence about what belongs here. The model compares documents with it.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>How much a wrong filing would matter. High means a person must always confirm.</summary>
    public ConsequenceLevel? Consequence { get; init; }

    /// <summary>Words or phrases that suggest this category.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Other names for the category itself ("BOQ" for a quote). Counted like keywords.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Regular expressions that strongly suggest this category. Each match counts double.</summary>
    public IReadOnlyList<string> Patterns { get; init; } = [];

    /// <summary>The "what is it about" chips shown to the user, such as Bricks, Cement, Labour.</summary>
    public IReadOnlyList<string> SubjectChips { get; init; } = [];

    /// <summary>The standard fields a document of this category normally has (party, amount, date ...).</summary>
    public IReadOnlyList<string> ExpectedFields { get; init; } = [];

    /// <summary>The fields saved when a person confirms a document of this category.</summary>
    public IReadOnlyList<FieldDefinition> FieldsToSave { get; init; } = [];

    /// <summary>The score at or above which TUMIIA is sure. 0.75 unless the category says otherwise.</summary>
    public double HighThreshold { get; init; } = 0.75;

    /// <summary>The score at or above which TUMIIA is fairly sure. 0.50 unless the category says otherwise.</summary>
    public double MediumThreshold { get; init; } = 0.50;

    /// <summary>True for the Needs Review category, which is never proposed by the rules or the model.</summary>
    public bool IsNeedsReview => Key == NeedsReviewKey;
}
