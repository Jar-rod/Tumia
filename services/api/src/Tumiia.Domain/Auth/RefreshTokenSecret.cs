using System.Security.Cryptography;
using System.Text;

namespace Tumiia.Domain.Auth;

/// <summary>
/// The secret value of a refresh token: 256 random bits, sent to the browser in a cookie. Only its hash is stored.
/// </summary>
/// <param name="Value">The token itself. Goes in the cookie; never stored or logged.</param>
/// <param name="Hash">Hex SHA-256 of <paramref name="Value"/>. This is what the database keeps.</param>
public sealed record RefreshTokenSecret(string Value, string Hash)
{
    private const int SizeInBytes = 32;

    /// <summary>Makes a new random token with a cryptographically secure generator.</summary>
    public static RefreshTokenSecret Generate()
    {
        var value = Base64UrlEncode(RandomNumberGenerator.GetBytes(SizeInBytes));
        return new RefreshTokenSecret(value, HashOf(value));
    }

    /// <summary>The hash to look a presented token up by.</summary>
    public static string HashOf(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    // URL-safe base64 without padding, so the value can sit in a cookie as it is.
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
