using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tumiia.Application.Auth;

namespace Tumiia.Infrastructure.Identity;

/// <summary>Creates signed access tokens (JWT, HS256) for signed-in users.</summary>
public sealed class JwtAccessTokenIssuer(IOptions<JwtSettings> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>
    /// A token valid for <see cref="JwtSettings.AccessTokenMinutes"/> minutes, carrying the user id (<c>sub</c>),
    /// email, roles and a unique token id (<c>jti</c>).
    /// </summary>
    public AccessToken Issue(UserAccount user)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(Claims(user)),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    private static List<Claim> Claims(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimNames.Subject, user.Id.ToString()),
            new(JwtClaimNames.Email, user.Email),
            new(JwtClaimNames.TokenId, Guid.NewGuid().ToString()),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(JwtClaimNames.Role, role)));
        return claims;
    }
}
