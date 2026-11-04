using Microsoft.AspNetCore.Http;
using Tumiia.Api.Logging;

namespace Tumiia.UnitTests.Logging;

public class CorrelationIdMiddlewareTests
{
    [Theory]
    [InlineData("abc-123")]
    [InlineData("A_b_C")]
    public void ResolveCorrelationId_WhenIncomingIdIsSafe_ReturnsIt(string incoming)
    {
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(incoming);

        correlationId.ShouldBe(incoming);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("new\nline")]
    [InlineData("<script>")]
    public void ResolveCorrelationId_WhenIncomingIdIsMissingOrUnsafe_ReturnsNewId(string? incoming)
    {
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(incoming);

        correlationId.ShouldNotBe(incoming);
        correlationId.Length.ShouldBe(32);
    }

    [Fact]
    public void ResolveCorrelationId_WhenIncomingIdIsTooLong_ReturnsNewId()
    {
        var incoming = new string('a', 65);

        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(incoming);

        correlationId.ShouldNotBe(incoming);
    }

    [Fact]
    public async Task InvokeAsync_WhenCalled_StoresIdForTheRestOfTheRequestAndCallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc-123";
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
        context.Items[CorrelationIdMiddleware.ItemKey].ShouldBe("abc-123");
    }
}
