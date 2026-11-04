using Serilog.Context;

namespace Tumiia.Api.Logging;

/// <summary>
/// Gives every request a correlation id so all log lines for one request can be found together.
/// Uses the caller's <c>X-Correlation-Id</c> header when it is safe, otherwise makes a new id,
/// and sends the id back in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The header that carries the correlation id in both directions.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The key under which the id is kept in <see cref="HttpContext.Items"/> for the rest of the request.</summary>
    public const string ItemKey = "CorrelationId";

    private const int MaxLength = 64;

    /// <summary>Sets the correlation id for this request, then runs the rest of the pipeline with it attached to every log line.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].ToString());
        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Returns the incoming id if it is short and only letters, digits, '-' or '_' (so it cannot
    /// inject anything into logs); otherwise returns a new random id.
    /// </summary>
    public static string ResolveCorrelationId(string? incoming)
    {
        if (IsSafe(incoming))
        {
            return incoming!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsSafe(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        return value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }
}
