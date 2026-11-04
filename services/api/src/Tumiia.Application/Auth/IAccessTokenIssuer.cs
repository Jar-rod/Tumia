namespace Tumiia.Application.Auth;

/// <summary>Creates access tokens (JWTs). Implemented in Infrastructure.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>Creates a 15-minute access token carrying the user's id, email and roles.</summary>
    AccessToken Issue(UserAccount user);
}
