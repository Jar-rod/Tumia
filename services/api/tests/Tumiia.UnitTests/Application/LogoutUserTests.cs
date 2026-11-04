using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Auth;

namespace Tumiia.UnitTests.Application;

public class LogoutUserTests
{
    private readonly AuthTestDoubles _fakes = new();

    private RefreshTokenSecret StoreTokenFor(Guid userId)
    {
        var secret = RefreshTokenSecret.Generate();
        var token = RefreshToken.StartFamily(userId, secret.Hash, _fakes.Clock.GetUtcNow(), TimeSpan.FromDays(14));
        _fakes.RefreshTokens.FindByHashAsync(secret.Hash, Arg.Any<CancellationToken>()).Returns(token);
        return secret;
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenBelongsToTheUser_RevokesItsFamilyAndRecordsLogout()
    {
        var secret = StoreTokenFor(AuthTestDoubles.Alice.Id);

        await new LogoutUser(_fakes.RefreshTokens, _fakes.AuditLog, _fakes.Clock).ExecuteAsync(AuthTestDoubles.Alice.Id, secret.Value, "10.0.0.1", CancellationToken.None);

        await _fakes.RefreshTokens.Received().RevokeFamilyAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LoggedOut, AuthTestDoubles.Alice.Id, "10.0.0.1", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenBelongsToSomeoneElse_LeavesItAlone()
    {
        var secret = StoreTokenFor(Guid.NewGuid());

        await new LogoutUser(_fakes.RefreshTokens, _fakes.AuditLog, _fakes.Clock).ExecuteAsync(AuthTestDoubles.Alice.Id, secret.Value, "10.0.0.1", CancellationToken.None);

        await _fakes.RefreshTokens.DidNotReceive().RevokeFamilyAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
