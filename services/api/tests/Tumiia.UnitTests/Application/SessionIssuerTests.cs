using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Domain.Auth;

namespace Tumiia.UnitTests.Application;

public class SessionIssuerTests
{
    private readonly AuthTestDoubles _fakes = new();

    [Fact]
    public async Task StartAsync_WhenCalled_StoresOnlyTheHashOfTheRefreshToken()
    {
        RefreshToken? stored = null;
        await _fakes.RefreshTokens.AddAsync(Arg.Do<RefreshToken>(token => stored = token), Arg.Any<CancellationToken>());

        var session = await _fakes.Sessions().StartAsync(AuthTestDoubles.Alice, CancellationToken.None);

        stored.ShouldNotBeNull();
        stored.TokenHash.ShouldBe(session.RefreshToken.Hash);
        stored.TokenHash.ShouldNotBe(session.RefreshToken.Value);
        stored.ExpiresAt.ShouldBe(_fakes.Clock.GetUtcNow().AddDays(14));
    }

    [Fact]
    public async Task StartAsync_WhenEmailIsAConfiguredAdmin_AddsTheAdminRole()
    {
        _fakes.Settings = new AuthSettings
        {
            AppBaseUrl = "https://app.example.test",
            AdminEmails = new HashSet<string>(["ALICE@example.test"], StringComparer.OrdinalIgnoreCase),
        };

        var session = await _fakes.Sessions().StartAsync(AuthTestDoubles.Alice, CancellationToken.None);

        session.User.Roles.ShouldContain(Roles.Admin);
        await _fakes.Users.Received().AddToRoleAsync(AuthTestDoubles.Alice.Id, Roles.Admin, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenNotAnAdmin_LeavesRolesAlone()
    {
        var session = await _fakes.Sessions().StartAsync(AuthTestDoubles.Alice, CancellationToken.None);

        session.User.Roles.ShouldBe([Roles.User]);
        await _fakes.Users.DidNotReceive().AddToRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
