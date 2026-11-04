using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tumiia.Application.Auth;
using Tumiia.Domain.Auth;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.UnitTests.Infrastructure;

public class JwtAccessTokenIssuerTests
{
    private static readonly UserAccount Admin = new(Guid.NewGuid(), "admin@example.test", "Admin", [Roles.User, Roles.Admin]);

    private readonly JwtSettings _settings = new() { SigningKey = new string('k', 48) };

    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UtcNow);

    private AccessToken Issue() => new JwtAccessTokenIssuer(Options.Create(_settings), _clock).Issue(Admin);

    private static Task<TokenValidationResult> ValidateAsync(string token, JwtSettings settings)
    {
        return new JsonWebTokenHandler().ValidateTokenAsync(token, settings.CreateValidationParameters());
    }

    [Fact]
    public async Task Issue_WhenCalled_CreatesATokenTheApiAccepts()
    {
        var token = Issue();

        var result = await ValidateAsync(token.Value, _settings);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Issue_WhenCalled_CarriesUserIdEmailRolesAndAUniqueId()
    {
        var token = Issue();

        var claims = (await ValidateAsync(token.Value, _settings)).ClaimsIdentity.Claims.ToList();

        claims.Single(claim => claim.Type == "sub").Value.ShouldBe(Admin.Id.ToString());
        claims.Single(claim => claim.Type == "email").Value.ShouldBe(Admin.Email);
        claims.Where(claim => claim.Type == "role").Select(claim => claim.Value).ShouldBe([Roles.User, Roles.Admin], ignoreOrder: true);
        claims.ShouldContain(claim => claim.Type == "jti");
    }

    [Fact]
    public void Issue_WhenCalled_ExpiresAfterFifteenMinutes()
    {
        var token = Issue();

        token.ExpiresAt.ShouldBe(_clock.GetUtcNow().AddMinutes(15));
    }

    [Fact]
    public async Task Validation_WhenSignedWithAnotherKey_RejectsTheToken()
    {
        var token = Issue();

        var result = await ValidateAsync(token.Value, new JwtSettings { SigningKey = new string('x', 48) });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ValidationParameters_WhenCreated_AllowOnlyThirtySecondsOfClockDifferenceAndHs256()
    {
        var parameters = _settings.CreateValidationParameters();

        parameters.ClockSkew.ShouldBe(TimeSpan.FromSeconds(30));
        parameters.ValidAlgorithms.ShouldBe([SecurityAlgorithms.HmacSha256]);
        parameters.ValidateIssuer.ShouldBeTrue();
        parameters.ValidateAudience.ShouldBeTrue();
        parameters.ValidateLifetime.ShouldBeTrue();
    }
}
