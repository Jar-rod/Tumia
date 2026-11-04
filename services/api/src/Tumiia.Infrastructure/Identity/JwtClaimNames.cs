namespace Tumiia.Infrastructure.Identity;

/// <summary>The claim names inside TUMIIA access tokens. Short JWT names, not the long .NET ones.</summary>
public static class JwtClaimNames
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string Role = "role";

    public const string TokenId = "jti";
}
