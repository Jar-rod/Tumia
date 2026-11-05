namespace Tumiia.Api.Dtos;

/// <summary>One answer per requested file, in the order sent.</summary>
/// <param name="Files">The answers.</param>
public sealed record UploadRequestsResponse(IReadOnlyList<UploadTargetResponse> Files);
