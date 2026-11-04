using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;

namespace Tumiia.UnitTests.Application;

public class LoginUserTests
{
    private readonly AuthTestDoubles _fakes = new();

    private LoginUser CreateLogin() => new(_fakes.Users, _fakes.Sessions(), _fakes.AuditLog);

    private void PasswordCheckReturns(PasswordCheckResult result)
    {
        _fakes.Users.CheckPasswordAsync(AuthTestDoubles.Alice.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsRight_StartsASessionAndRecordsSuccess()
    {
        PasswordCheckReturns(PasswordCheckResult.Succeeded);

        var session = await CreateLogin().ExecuteAsync("alice@example.test", "Correct-Pass1", "10.0.0.1", CancellationToken.None);

        session.ShouldNotBeNull();
        session.User.ShouldBe(AuthTestDoubles.Alice);
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LoginSucceeded, AuthTestDoubles.Alice.Id, "10.0.0.1", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsUnknown_FailsAndRecordsAFailureWithNoUser()
    {
        var session = await CreateLogin().ExecuteAsync("nobody@example.test", "whatever", "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LoginFailed, null, "10.0.0.1", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWrong_FailsAndRecordsLoginFailed()
    {
        PasswordCheckReturns(PasswordCheckResult.WrongPassword);

        var session = await CreateLogin().ExecuteAsync("alice@example.test", "wrong", "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LoginFailed, AuthTestDoubles.Alice.Id, "10.0.0.1", false, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.DidNotReceive().RecordAsync(AuditEventType.LockedOut, Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheAttemptLocksTheAccount_RecordsLoginFailedAndLockedOut()
    {
        PasswordCheckReturns(PasswordCheckResult.JustLockedOut);

        var session = await CreateLogin().ExecuteAsync("alice@example.test", "wrong", "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LoginFailed, AuthTestDoubles.Alice.Id, "10.0.0.1", false, Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LockedOut, AuthTestDoubles.Alice.Id, "10.0.0.1", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccountIsLocked_FailsEvenWithTheRightPassword()
    {
        PasswordCheckReturns(PasswordCheckResult.LockedOut);

        var session = await CreateLogin().ExecuteAsync("alice@example.test", "Correct-Pass1", "10.0.0.1", CancellationToken.None);

        session.ShouldBeNull();
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.LockedOut, AuthTestDoubles.Alice.Id, "10.0.0.1", false, Arg.Any<CancellationToken>());
    }
}
