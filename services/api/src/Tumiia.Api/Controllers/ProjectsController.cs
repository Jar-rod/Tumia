using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tumiia.Api.Auth;
using Tumiia.Api.Dtos;
using Tumiia.Api.RateLimiting;
using Tumiia.Api.Security;
using Tumiia.Application.Documents;
using Tumiia.Application.Projects;
using Tumiia.Domain.Documents;

namespace Tumiia.Api.Controllers;

/// <summary>Project vaults, and uploading files into them. Only members of a project can see it.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private const string ProjectNotFound = "Project not found.";

    /// <summary>Creates a project vault. The caller becomes its owner.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="createProject">The create project use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<ProjectResponse>> CreateAsync(CreateProjectRequest request, [FromServices] CreateProject createProject, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["name"] = ["The project needs a name."] }));
        }

        var created = await createProject.ExecuteAsync(User.GetUserId(), request.Name, cancellationToken);
        var response = ToResponse(created);
        return Created($"/api/v1/projects/{response.Id}", response);
    }

    /// <summary>Lists the caller's projects, newest first.</summary>
    /// <param name="listMyProjects">The list my projects use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> ListAsync([FromServices] ListMyProjects listMyProjects, CancellationToken cancellationToken)
    {
        var projects = await listMyProjects.ExecuteAsync(User.GetUserId(), cancellationToken);
        return Ok(projects.Select(ToResponse).ToList());
    }

    /// <summary>Gets one project.</summary>
    /// <remarks>404 when it does not exist or the caller is not a member (the two are not told apart).</remarks>
    /// <param name="projectId">The project id.</param>
    /// <param name="getProject">The get project use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<ProjectResponse>> GetAsync(Guid projectId, [FromServices] GetProject getProject, CancellationToken cancellationToken)
    {
        var project = await getProject.ExecuteAsync(User.GetUserId(), projectId, cancellationToken);
        return project is null ? Problem(title: ProjectNotFound, statusCode: StatusCodes.Status404NotFound) : Ok(ToResponse(project));
    }

    /// <summary>Step 1 of uploading: get an upload address for each file.</summary>
    /// <remarks>
    /// Send up to 50 files' names, types, sizes and SHA-256 hashes. Each new file gets a document id and a 5-minute
    /// pre-signed address to PUT the file to, with exactly that Content-Type and size. A file already in the project
    /// gets <c>existingDocumentId</c> instead. Files with a type TUMIIA does not take, or over 50 MB, are refused
    /// with 400, and then nothing is created.
    /// </remarks>
    /// <param name="projectId">The project id.</param>
    /// <param name="request">The request body.</param>
    /// <param name="requestUploads">The request uploads use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("{projectId:guid}/documents/upload-requests")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [ProducesResponseType<UploadRequestsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<UploadRequestsResponse>> RequestUploadsAsync(Guid projectId, UploadRequestsRequest request, [FromServices] RequestUploads requestUploads, CancellationToken cancellationToken)
    {
        var files = request.Files.Select(file => new UploadFile(file.FileName, file.ContentType, file.SizeBytes, file.Sha256)).ToList();
        var result = await requestUploads.ExecuteAsync(User.GetUserId(), projectId, files, request.CaptureSource, ClientIpResolver.Resolve(HttpContext), cancellationToken);

        if (!result.ProjectFound)
        {
            return Problem(title: ProjectNotFound, statusCode: StatusCodes.Status404NotFound);
        }

        if (result.Errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary()));
        }

        var targets = result.Targets.Select(target => new UploadTargetResponse(target.FileName, target.DocumentId, target.UploadUrl, target.ExistingDocumentId));
        return Ok(new UploadRequestsResponse(targets.ToList()));
    }

    /// <summary>Lists a project's documents, newest first.</summary>
    /// <remarks>Filter by <c>status</c> (for example NeedsReview) and by <c>category</c> (the confirmed document type). Unfinished uploads are left out unless you ask for status PendingUpload.</remarks>
    /// <param name="projectId">The project id.</param>
    /// <param name="status">Only documents with this status.</param>
    /// <param name="category">Only documents with this confirmed document type.</param>
    /// <param name="listDocuments">The list documents use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet("{projectId:guid}/documents")]
    [ProducesResponseType<IReadOnlyList<DocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> ListDocumentsAsync(
        Guid projectId,
        [FromQuery] DocumentStatus? status,
        [FromQuery] string? category,
        [FromServices] ListDocuments listDocuments,
        CancellationToken cancellationToken)
    {
        var documents = await listDocuments.ExecuteAsync(User.GetUserId(), projectId, status, category, cancellationToken);
        if (documents is null)
        {
            return Problem(title: ProjectNotFound, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(documents.Select(DocumentResponse.From).ToList());
    }

    private static ProjectResponse ToResponse(ProjectSummary summary)
    {
        return new ProjectResponse(summary.Project.Id, summary.Project.Name, summary.Project.CreatedAt, summary.Role);
    }
}
