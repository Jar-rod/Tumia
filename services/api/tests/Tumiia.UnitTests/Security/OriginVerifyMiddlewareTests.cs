using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tumiia.Api.Security;

namespace Tumiia.UnitTests.Security;

public class OriginVerifyMiddlewareTests
{
    private const string Secret = "cloudfront-secret";

    private bool _nextCalled;

    private OriginVerifyMiddleware Create(string environmentName, string? secret = Secret)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new OriginVerifyMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new OriginVerifySettings { Secret = secret }),
            environment,
            NullLogger<OriginVerifyMiddleware>.Instance);
    }

    private static DefaultHttpContext Request(string path, string? header = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (header is not null)
        {
            context.Request.Headers[OriginVerifyMiddleware.HeaderName] = header;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderIsMissingInProduction_Answers403()
    {
        var context = Request("/api/v1/auth/me");

        await Create(Environments.Production).InvokeAsync(context, Substitute.For<IProblemDetailsService>());

        context.Response.StatusCode.ShouldBe(403);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderIsRight_LetsTheRequestThroughAndMarksItVerified()
    {
        var context = Request("/api/v1/auth/me", Secret);

        await Create(Environments.Production).InvokeAsync(context, Substitute.For<IProblemDetailsService>());

        _nextCalled.ShouldBeTrue();
        context.Items[OriginVerifyMiddleware.VerifiedItemKey].ShouldBe(true);
    }

    [Fact]
    public async Task InvokeAsync_WhenLoadBalancerChecksHealth_LetsItThroughWithoutTheHeader()
    {
        var context = Request("/api/v1/health");

        await Create(Environments.Production).InvokeAsync(context, Substitute.For<IProblemDetailsService>());

        _nextCalled.ShouldBeTrue();
        context.Items.ContainsKey(OriginVerifyMiddleware.VerifiedItemKey).ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoSecretIsConfigured_RejectsEverythingExceptHealth()
    {
        var context = Request("/api/v1/auth/me", "anything");

        await Create(Environments.Production, secret: null).InvokeAsync(context, Substitute.For<IProblemDetailsService>());

        context.Response.StatusCode.ShouldBe(403);
    }

    [Fact]
    public async Task InvokeAsync_WhenRunningLocally_DoesNotCheck()
    {
        var context = Request("/api/v1/auth/me");

        await Create(Environments.Development).InvokeAsync(context, Substitute.For<IProblemDetailsService>());

        _nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(Secret, Secret, true)]
    [InlineData("wrong", Secret, false)]
    [InlineData("", Secret, false)]
    [InlineData("", "", false)]
    [InlineData(Secret, null, false)]
    public void IsValid_WhenComparingHeaderAndSecret_OnlyMatchesTheExactNonEmptySecret(string presented, string? secret, bool expected)
    {
        var valid = OriginVerifyMiddleware.IsValid(presented, secret);

        valid.ShouldBe(expected);
    }
}
