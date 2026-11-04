using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Audit;

namespace Tumiia.UnitTests.Application;

public class RegisterUserTests
{
    private readonly AuthTestDoubles _fakes = new();

    private RegisterUser CreateRegister() => new(_fakes.Users, _fakes.Sessions(), _fakes.AuditLog);

    [Fact]
    public async Task ExecuteAsync_WhenAccountIsCreated_SignsTheUserInAndRecordsIt()
    {
        _fakes.Users.CreateAsync("alice@example.test", "Correct-Pass1", "Alice", Arg.Any<CancellationToken>())
            .Returns(CreateUserResult.Created(AuthTestDoubles.Alice));

        var result = await CreateRegister().ExecuteAsync(" alice@example.test ", "Correct-Pass1", " Alice ", "10.0.0.1", CancellationToken.None);

        result.Session.ShouldNotBeNull();
        await _fakes.AuditLog.Received().RecordAsync(AuditEventType.Registered, AuthTestDoubles.Alice.Id, "10.0.0.1", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsTaken_ReportsItWithoutASession()
    {
        _fakes.Users.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CreateUserResult(null, true, new Dictionary<string, string[]>()));

        var result = await CreateRegister().ExecuteAsync("alice@example.test", "Correct-Pass1", "Alice", "10.0.0.1", CancellationToken.None);

        result.Session.ShouldBeNull();
        result.DuplicateEmail.ShouldBeTrue();
        await _fakes.AuditLog.DidNotReceive().RecordAsync(AuditEventType.Registered, Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
