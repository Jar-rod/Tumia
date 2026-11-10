using Tumiia.Domain.Analysis;

namespace Tumiia.Api.Dtos;

/// <summary>One fact the AI found. These are suggestions until a person confirms them.</summary>
/// <param name="Name">The field, such as document_type, party, amount, currency, document_date, subject.</param>
/// <param name="Value">The value: dates as yyyy-MM-dd, amounts as plain numbers ("18450.00"), currencies as ISO codes ("ZAR").</param>
/// <param name="Confidence">From 0 to 1, for this field alone. Show it as words, not as a number.</param>
/// <param name="Band">The confidence as High, Medium or Low (High at 0.75 or more, Medium at 0.50 or more).</param>
/// <param name="Evidence">Where it was found.</param>
/// <param name="Method">How it was found: hosted (the AI on Bedrock), regex, keyword or exif.</param>
public sealed record ExtractedFieldResponse(string Name, string Value, double Confidence, ConfidenceBand Band, FieldEvidenceResponse Evidence, string Method)
{
    /// <summary>Copies one extracted field.</summary>
    public static ExtractedFieldResponse From(ExtractedField field) => new(
        field.Name,
        field.Value,
        Math.Round(field.Confidence, 4),
        ConfidenceCalculator.BandFor(field.Confidence, 0.75, 0.50),
        new FieldEvidenceResponse(field.Evidence.Source, field.Evidence.Page, field.Evidence.Line, field.Evidence.Snippet),
        field.Method);
}
