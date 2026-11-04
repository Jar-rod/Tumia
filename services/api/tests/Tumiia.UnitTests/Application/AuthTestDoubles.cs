using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Auth;
using Tumiia.Domain.Email;

namespace Tumiia.UnitTests.Application;

/// <summary>The fakes every auth use-case test needs, wired together.</summary>
public sealed class AuthTestDoubles
{
    public static readonly UserAccount Alice = new(Guid.NewGuid(), "alice@example.test", "Alice", [Roles.User]);

    public IUserAccounts Users { get; } = Substitute.For<IUserAccounts>();

    public IRefreshTokenStore RefreshTokens { get; } = Substitute.For<IRefreshTokenStore>();

    public IAccessTokenIssuer AccessTokens { get; } = Substitute.For<IAccessTokenIssuer>();

    public IAuditLog AuditLog { get; } = Substitute.For<IAuditLog>();

    public IEmailSender Email { get; } = Substitute.For<IEmailSender>();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

    public AuthSettings Settings { get; set; } = new() { AppBaseUrl = "https://app.example.test" };

    public AuthTestDoubles()
    {
        AccessTokens.Issue(Arg.Any<UserAccount>()).Returns(new AccessToken("jwt", DateTimeOffset.MaxValue));
        Users.FindByEmailAsync(Alice.Email, Arg.Any<CancellationToken>()).Returns(Alice);
        Users.FindByIdAsync(Alice.Id, Arg.Any<CancellationToken>()).Returns(Alice);
    }

    public SessionIssuer Sessions() => new(Users, RefreshTokens, AccessTokens, Settings, Clock);
}
