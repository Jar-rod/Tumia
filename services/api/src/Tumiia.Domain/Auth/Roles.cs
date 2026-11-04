namespace Tumiia.Domain.Auth;

/// <summary>The roles a user can have. Everyone is a User; Admins can also change the taxonomy and export feedback.</summary>
public static class Roles
{
    public const string User = "User";

    public const string Admin = "Admin";
}
