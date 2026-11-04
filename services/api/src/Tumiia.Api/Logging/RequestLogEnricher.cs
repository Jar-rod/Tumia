using System.Security.Claims;
using Asp.Versioning;
using Serilog;

namespace Tumiia.Api.Logging;

/// <summary>
/// Adds the extra fields to the one log line written for every request: route template, API version,
/// user id and client IP. Never adds bodies, headers, cookies or tokens.
/// </summary>
public static class RequestLogEnricher
{
    /// <summary>Called by Serilog's request logging once the request has finished.</summary>
    public static void Enrich(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("RouteTemplate", GetRouteTemplate(httpContext));
        diagnosticContext.Set("ApiVersion", GetApiVersion(httpContext));
        diagnosticContext.Set("UserId", GetUserId(httpContext.User));
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    /// <summary>
    /// Returns the route pattern (for example <c>api/v{version:apiVersion}/health</c>) rather than the raw path,
    /// so ids in the URL do not end up in the logs. Returns "unmatched" when no route was found.
    /// </summary>
    public static string GetRouteTemplate(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint() is RouteEndpoint endpoint)
        {
            return endpoint.RoutePattern.RawText ?? "unmatched";
        }

        return "unmatched";
    }

    /// <summary>
    /// Returns the API version the request asked for (for example "1"), or "none" when the request never
    /// reached a versioned route.
    /// </summary>
    public static string GetApiVersion(HttpContext httpContext)
    {
        var versioning = httpContext.Features.Get<IApiVersioningFeature>();
        return versioning?.RequestedApiVersion?.ToString() ?? "none";
    }

    /// <summary>Returns the signed-in user's id from the <c>sub</c> claim, or "anonymous".</summary>
    public static string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "anonymous";
    }
}
