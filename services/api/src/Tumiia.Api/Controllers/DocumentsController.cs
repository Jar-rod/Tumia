using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Tumiia.Api.Auth;
using Tumiia.Api.Dtos;
using Tumiia.Api.Security;
using Tumiia.Application.Documents;

namespace Tumiia.Api.Controllers;

/// <summary>
/// One document: finishing its upload, reading it, and getting its original file. A document in a project the
/// caller does not belong to answers 404, exactly like one that does not exist.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
[Produces("application/json")]
public sealed class DocumentsController : ControllerBase
{
    private const string DocumentNotFound = "Document not found.";

    /// <summary>Step 2 of uploading: tell the API the file has been PUT to its upload address.</summary>
    /// <remarks>
    /// The API checks the file is in storage and is the announced size, marks the document New and queues it for
    /// analysis. 409 when the file has not arrived or is the wrong size. Safe to call again.
    /// </remarks>
    /// <param name="documentId">The document id.</param>
    /// <param name="confirmUpload">The confirm upload use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("{documentId:guid}/uploaded")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<DocumentResponse>> UploadedAsync(Guid documentId, [FromServices] ConfirmUpload confirmUpload, CancellationToken cancellationToken)
    {
        var result = await confirmUpload.ExecuteAsync(User.GetUserId(), documentId, ClientIpResolver.Resolve(HttpContext), cancellationToken);
        return result.Outcome switch
        {
            ConfirmUploadOutcome.Queued => Ok(DocumentResponse.From(result.Document!)),
            ConfirmUploadOutcome.FileMissing => Problem(title: "The file has not arrived yet. Upload it to its upload address first.", statusCode: StatusCodes.Status409Conflict),
            ConfirmUploadOutcome.SizeMismatch => Problem(title: "The uploaded file is not the size that was announced. Please upload it again.", statusCode: StatusCodes.Status409Conflict),
            _ => Problem(title: DocumentNotFound, statusCode: StatusCodes.Status404NotFound),
        };
    }

    /// <summary>Gets one document with everything known about it.</summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="getDocument">The get document use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<DocumentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<DocumentDetailResponse>> GetAsync(Guid documentId, [FromServices] GetDocument getDocument, CancellationToken cancellationToken)
    {
        var document = await getDocument.ExecuteAsync(User.GetUserId(), documentId, cancellationToken);
        return document is null ? Problem(title: DocumentNotFound, statusCode: StatusCodes.Status404NotFound) : Ok(DocumentDetailResponse.From(document));
    }

    /// <summary>Gets a 5-minute address to view or download the original file.</summary>
    /// <remarks>PDFs and common images open in the browser; other files download under their original name. 409 when the file has not been uploaded yet.</remarks>
    /// <param name="documentId">The document id.</param>
    /// <param name="getSourceUrl">The get document source url use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet("{documentId:guid}/source-url")]
    [ProducesResponseType<SourceUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<SourceUrlResponse>> GetSourceUrlAsync(Guid documentId, [FromServices] GetDocumentSourceUrl getSourceUrl, CancellationToken cancellationToken)
    {
        var result = await getSourceUrl.ExecuteAsync(User.GetUserId(), documentId, cancellationToken);
        if (!result.Found)
        {
            return Problem(title: DocumentNotFound, statusCode: StatusCodes.Status404NotFound);
        }

        if (result.Url is null)
        {
            return Problem(title: "The file has not been uploaded yet.", statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(new SourceUrlResponse(result.Url, result.ExpiresAt!.Value));
    }
}
