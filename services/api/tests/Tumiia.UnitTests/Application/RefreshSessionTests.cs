using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Auth;

namespace Tumiia.UnitTests.Application;

public class RefreshSessionTests
{
    private readonly AuthTestDoubles _fakes = new();

    private RefreshSession CreateRefresh() =>
        new(_fakes.RefreshTokens, _fakes.Users, _fakes.AccessTokens, _fakes.AuditLog, _fakes.Settings, _fakes.Clock);

    /// <summary>A stored token for Alice, and the secret value the browser holds for it.</summary>
    private (RefreshToken Token, string Value) StoredToken()
    {
        var secret = RefreshTokenSecret.Generate();
        var token = RefreshToken.StartFamily(AuthTestDoubles.Alice.Id, secret.Hash, _fakes.Clock.GetUtcNow(), TimeSpan.FromDays(14));
        _fakes.RefreshTokens.FindByHashAsync(secret.Hash, Arg.Any<CancellationToken>()).Returns(token);
        return (token, secret.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsActive_RotatesItAndReturnsANewSession()
    {
        var (token, value) = StoredToken();

        var session = await CreateRefresh().ExecuteAsync(value, "10.0.0.1", CancellationToken.None);

        session.ShouldNotBeNull();
        session.RefreshToken.Value.ShouldNotBe(value);
        token.IsReuse().ShouldBeTrue();
        await _fakes.RefreshTokens.Received().SaveRotationAsync(token, Arg.Is<RefreshToken>(next => next.FamilyId == token.FamilyId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnAlreadyUsedTokenIsPresented_RevokesTheWholeFamily()
    {
        var (token, value) = StoredToken();
        token.Rotate("next-hash", _fakes.Clock.GetUtcNow(), TimeSpan.FromDays(14));

        var session = await CreateRefresh().ExecuteAsync(value, "10.0.0.9", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.RefreshTokens.Received().RevokeFamilyAsync(token.FamilyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.RefreshTokenReuseDetected, AuthTestDoubles.Alice.Id, "10.0.0.9", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenHasExpired_FailsWithoutRevokingAnything()
    {
        var (_, value) = StoredToken();
        _fakes.Clock.Advance(TimeSpan.FromDays(15));

        var session = await CreateRefresh().ExecuteAsync(value, "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.RefreshTokens.DidNotReceive().RevokeFamilyAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public async Task ExecuteAsync_WhenTokenIsMissingOrUnknown_Fails(string? value)
    {
        var session = await CreateRefresh().ExecuteAsync(value, "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
    }
}
