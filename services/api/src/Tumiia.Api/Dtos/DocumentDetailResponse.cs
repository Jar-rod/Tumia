using System.Text.Json;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.Api.Dtos;

/// <summary>
/// Everything about one document, including the AI's latest analysis (proposed category, fields and their
/// confidence) once it has run.
/// </summary>
/// <param name="Document">The fields shown in lists.</param>
/// <param name="Sha256">The file's SHA-256.</param>
/// <param name="ContentTypeDetected">The real file type, once analysed.</param>
/// <param name="ExtensionMatches">Whether the real type matches the extension, once analysed.</param>
/// <param name="UploadedBy">Who uploaded it.</param>
/// <param name="CorrectionFlag">True once a person changed something the AI proposed.</param>
/// <param name="ConfirmedBy">Who confirmed it, if anyone.</param>
/// <param name="ConfirmedAt">When it was confirmed (UTC).</param>
/// <param name="ConfirmedFields">The confirmed fields, as JSON, once confirmed.</param>
/// <param name="FailureReason">Why the analysis gave up, when the status is Failed.</param>
/// <param name="Analysis">The latest analysis, or null while the document is New or Processing.</param>
public sealed record DocumentDetailResponse(
    DocumentResponse Document,
    string Sha256,
    string? ContentTypeDetected,
    bool? ExtensionMatches,
    Guid UploadedBy,
    bool CorrectionFlag,
    Guid? ConfirmedBy,
    DateTimeOffset? ConfirmedAt,
    JsonElement? ConfirmedFields,
    string? FailureReason,
    AnalysisResponse? Analysis)
{
    /// <summary>Copies everything the API may show about a document (never its storage key) and its latest analysis.</summary>
    public static DocumentDetailResponse From(Document document, DocumentAnalysis? analysis) => new(
        DocumentResponse.From(document),
        document.Sha256,
        document.ContentTypeDetected,
        document.ExtensionMatches,
        document.UploadedBy,
        document.CorrectionFlag,
        document.ConfirmedBy,
        document.ConfirmedAt,
        document.ConfirmedFields is null ? null : JsonDocument.Parse(document.ConfirmedFields).RootElement.Clone(),
        document.FailureReason,
        analysis is null ? null : AnalysisResponse.From(analysis));
}
