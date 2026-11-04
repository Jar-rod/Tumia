using Serilog.Core;
using Serilog.Events;

namespace Tumiia.Api.Logging;

/// <summary>
/// Removes the raw request path that ASP.NET Core attaches to every log line. A path can contain ids
/// (for example <c>/documents/3f2a…</c>), so logs use the route template instead.
/// </summary>
public sealed class RemoveRawPathEnricher : ILogEventEnricher
{
    /// <summary>The property ASP.NET Core adds with the raw path.</summary>
    public const string PropertyName = "RequestPath";

    /// <summary>Drops the raw path from the log event, if it is there.</summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.RemovePropertyIfPresent(PropertyName);
    }
}
