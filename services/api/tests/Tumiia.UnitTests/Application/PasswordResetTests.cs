using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Email;

namespace Tumiia.UnitTests.Application;

public class PasswordResetTests
{
    private readonly AuthTestDoubles _fakes = new();

    private RequestPasswordReset CreateRequest() => new(_fakes.Users, _fakes.Email, _fakes.AuditLog, _fakes.Settings);

    private ResetPassword CreateReset() => new(_fakes.Users, _fakes.RefreshTokens, _fakes.AuditLog, _fakes.Clock);

    [Fact]
    public async Task RequestPasswordReset_WhenEmailHasAnAccount_EmailsAResetLink()
    {
        _fakes.Users.CreatePasswordResetTokenAsync(AuthTestDoubles.Alice.Id, Arg.Any<CancellationToken>()).Returns("tok/en+1");

        await CreateRequest().ExecuteAsync("alice@example.test", "10.0.0.1", CancellationToken.None);

        await _fakes.Email.Received().SendAsync(
            Arg.Is<EmailMessage>(email => email.To == "alice@example.test"
                && email.Body.Contains("https://app.example.test/reset-password?email=alice%40example.test&token=tok%2Fen%2B1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPasswordReset_WhenEmailIsUnknown_SendsNothingButStillRecordsTheRequest()
    {
        await CreateRequest().ExecuteAsync("nobody@example.test", "10.0.0.1", CancellationToken.None);

        await _fakes.Email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.PasswordResetRequested, null, "10.0.0.1", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WhenItWorks_SignsTheUserOutEverywhere()
    {
        _fakes.Users.ResetPasswordAsync(AuthTestDoubles.Alice.Id, "token", "New-Password1", Arg.Any<CancellationToken>()).Returns(ResetPasswordResult.Done);

        var result = await CreateReset().ExecuteAsync("alice@example.test", "token", "New-Password1", "10.0.0.1", CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        await _fakes.RefreshTokens.Received().RevokeAllForUserAsync(AuthTestDoubles.Alice.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.PasswordReset, AuthTestDoubles.Alice.Id, "10.0.0.1", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WhenTheLinkIsBad_KeepsExistingSessions()
    {
        _fakes.Users.ResetPasswordAsync(AuthTestDoubles.Alice.Id, "stale", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ResetPasswordResult.BadLink);

        var result = await CreateReset().ExecuteAsync("alice@example.test", "stale", "New-Password1", "10.0.0.1", CancellationToken.None);

        result.InvalidLink.ShouldBeTrue();
        await _fakes.RefreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WhenEmailIsUnknown_ReportsABadLink()
    {
        var result = await CreateReset().ExecuteAsync("nobody@example.test", "token", "New-Password1", "10.0.0.1", CancellationToken.None);

        result.InvalidLink.ShouldBeTrue();
    }
}
