using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tumiia.Api.ErrorHandling;

namespace Tumiia.UnitTests.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();

    private GlobalExceptionHandler CreateHandler(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        _problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(true);
        return new GlobalExceptionHandler(_problemDetailsService, environment, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_WhenNotDevelopment_WritesGeneric500WithoutExceptionDetails()
    {
        var handler = CreateHandler(Environments.Production);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("secret detail"), CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(500);
        await _problemDetailsService.Received().TryWriteAsync(Arg.Is<ProblemDetailsContext>(written =>
            written.ProblemDetails.Status == 500 && !written.ProblemDetails.Extensions.ContainsKey("exception")));
    }

    [Fact]
    public async Task TryHandleAsync_WhenDevelopment_IncludesExceptionDetails()
    {
        var handler = CreateHandler(Environments.Development);

        await handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("what went wrong"), CancellationToken.None);

        await _problemDetailsService.Received().TryWriteAsync(Arg.Is<ProblemDetailsContext>(written =>
            written.ProblemDetails.Extensions["exception"]!.ToString()!.Contains("what went wrong")));
    }
}
