using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Tumiia.Api.Logging;

namespace Tumiia.Api.Startup;

/// <summary>Sets up Serilog: compact JSON to the console, which CloudWatch Logs collects in AWS.</summary>
public static class LoggingSetup
{
    /// <summary>Replaces the default logging with Serilog. Levels come from the <c>Serilog</c> section of configuration.</summary>
    public static WebApplicationBuilder AddTumiiaLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.With<RemoveRawPathEnricher>()
            .WriteTo.Console(new CompactJsonFormatter()));

        return builder;
    }

    /// <summary>
    /// Writes one log line per request with the method, route template, status and duration, plus the
    /// fields from <see cref="RequestLogEnricher"/>. The raw path is left out (see <see cref="RemoveRawPathEnricher"/>)
    /// so ids in URLs are not logged.
    /// </summary>
    public static WebApplication UseTumiiaRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RouteTemplate} responded {StatusCode} in {Elapsed:0.0} ms";
            options.EnrichDiagnosticContext = RequestLogEnricher.Enrich;
            options.GetMessageTemplateProperties = (httpContext, _, elapsedMs, statusCode) =>
            [
                new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
                new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
            ];
        });

        return app;
    }
}
