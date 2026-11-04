using System.Net;

namespace Tumiia.Api.Security;

/// <summary>
/// Works out the caller's IP address, for rate limiting, logs and the audit trail. In AWS every request arrives
/// from CloudFront, so the real address is in CloudFront's <c>CloudFront-Viewer-Address</c> header. That header is
/// only trusted once <see cref="OriginVerifyMiddleware"/> has confirmed the request came through CloudFront;
/// otherwise anyone could fake it. Locally, the connection's own address is used.
/// </summary>
public static class ClientIpResolver
{
    /// <summary>CloudFront's header: the viewer's IP and port, for example <c>198.51.100.10:46532</c>.</summary>
    public const string ViewerAddressHeader = "CloudFront-Viewer-Address";

    /// <summary>Returns the caller's IP address, or "unknown".</summary>
    public static string Resolve(HttpContext context)
    {
        var verifiedByCloudFront = context.Items.TryGetValue(OriginVerifyMiddleware.VerifiedItemKey, out var verified) && verified is true;
        if (verifiedByCloudFront)
        {
            var viewerIp = ParseViewerAddress(context.Request.Headers[ViewerAddressHeader].ToString());
            if (viewerIp is not null)
            {
                return viewerIp;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Takes the IP out of an "ip:port" value. IPv6 addresses contain colons too, so the port is whatever follows
    /// the last colon. Returns null when the value is not a valid address.
    /// </summary>
    public static string? ParseViewerAddress(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        var lastColon = headerValue.LastIndexOf(':');
        if (lastColon <= 0)
        {
            return null;
        }

        var ipText = headerValue[..lastColon].Trim('[', ']');
        return IPAddress.TryParse(ipText, out var address) ? address.ToString() : null;
    }
}
