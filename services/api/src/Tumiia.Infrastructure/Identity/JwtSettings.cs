using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tumiia.Infrastructure.Identity;

/// <summary>
/// Access token settings, from the <c>Jwt</c> configuration section. The signing key comes from SSM Parameter
/// Store in AWS; locally a random key is made at start-up (so tokens stop working after a restart).
/// </summary>
public sealed class JwtSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Who issues the tokens.</summary>
    [Required]
    public string Issuer { get; set; } = "tumiia-api";

    /// <summary>Who the tokens are for.</summary>
    [Required]
    public string Audience { get; set; } = "tumiia-web";

    /// <summary>The HS256 signing key. At least 32 characters. Never commit one.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime. 15 minutes.</summary>
    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>The key used both to sign tokens and to check them.</summary>
    public SymmetricSecurityKey CreateSigningKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    }

    /// <summary>
    /// How incoming tokens are checked: issuer, audience, lifetime and signing key must all be right, only HS256 is
    /// accepted, and clocks may differ by at most 30 seconds.
    /// </summary>
    public TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtClaimNames.Subject,
            RoleClaimType = JwtClaimNames.Role,
        };
    }
}
