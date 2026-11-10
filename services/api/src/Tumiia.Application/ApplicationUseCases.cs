using Tumiia.Application.Analysis;
using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Application.Projects;

namespace Tumiia.Application;

/// <summary>Lists the use cases, so the API can register them with one call.</summary>
public static class ApplicationUseCases
{
    /// <summary>Every use-case class. Each is registered as a scoped service (one per request).</summary>
    public static readonly IReadOnlyList<Type> All =
    [
        typeof(SessionIssuer),
        typeof(RegisterUser),
        typeof(LoginUser),
        typeof(RefreshSession),
        typeof(LogoutUser),
        typeof(RequestPasswordReset),
        typeof(ResetPassword),
        typeof(GetCurrentUser),
        typeof(ProjectAccessGuard),
        typeof(CreateProject),
        typeof(ListMyProjects),
        typeof(GetProject),
        typeof(RequestUploads),
        typeof(ConfirmUpload),
        typeof(ListDocuments),
        typeof(GetDocument),
        typeof(GetDocumentSourceUrl),
        typeof(CapturePastedText),
        typeof(AnalyseDocument),
        typeof(FailDocumentAnalysis),
    ];
}
