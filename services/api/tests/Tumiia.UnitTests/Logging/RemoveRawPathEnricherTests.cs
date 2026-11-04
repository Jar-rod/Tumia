using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tumiia.Api.Logging;

namespace Tumiia.UnitTests.Logging;

public class RemoveRawPathEnricherTests
{
    [Fact]
    public void Enrich_WhenEventHasRawPath_RemovesItAndKeepsOtherProperties()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse("test"),
            [
                new LogEventProperty("RequestPath", new ScalarValue("/api/v1/documents/3f2a")),
                new LogEventProperty("RouteTemplate", new ScalarValue("api/v{version}/documents/{id}")),
            ]);

        new RemoveRawPathEnricher().Enrich(logEvent, Substitute.For<ILogEventPropertyFactory>());

        logEvent.Properties.ShouldNotContainKey("RequestPath");
        logEvent.Properties.ShouldContainKey("RouteTemplate");
    }
}
