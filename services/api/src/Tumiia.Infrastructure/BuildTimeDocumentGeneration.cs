using System.Reflection;

namespace Tumiia.Infrastructure;

/// <summary>
/// Tells whether the app is being started by the build only to write <c>openapi/v1.json</c>. That run has no
/// configuration (no JWT key, no database), so start-up checks that need real settings are skipped for it.
/// Checking the entry assembly name is the approach Microsoft documents for build-time OpenAPI generation.
/// </summary>
public static class BuildTimeDocumentGeneration
{
    /// <summary>True inside the build's OpenAPI generator, never when the app really runs.</summary>
    public static bool IsRunning => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
