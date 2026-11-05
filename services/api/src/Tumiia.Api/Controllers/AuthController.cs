using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tumiia.Api.Auth;
using Tumiia.Api.Dtos;
using Tumiia.Api.RateLimiting;
using Tumiia.Api.Security;
using Tumiia.Application.Auth;

namespace Tumiia.Api.Controllers;

/// <summary>Sign-up, sign-in, staying signed in, signing out, and resetting a forgotten password.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthSettings settings) : ControllerBase
{
    private const string LoginFailedMessage = "Email or password is incorrect.";

    /// <summary>Creates an account and signs the new user in.</summary>
    /// <remarks>Returns an access token in the body and sets the refresh token cookie. No email confirmation yet.</remarks>
    /// <param name="request">The request body.</param>
    /// <param name="registerUser">The register user use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(RegisterRequest request, [FromServices] RegisterUser registerUser, CancellationToken cancellationToken)
    {
        var result = await registerUser.ExecuteAsync(request.Email, request.Password, request.DisplayName, ClientIp, cancellationToken);
        if (result.DuplicateEmail)
        {
            return Problem(title: "An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        if (result.Session is null)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary()));
        }

        return SignedIn(result.Session);
    }

    /// <summary>Signs in with an email and password.</summary>
    /// <remarks>
    /// Returns an access token in the body and sets the refresh token cookie. A wrong email, a wrong password and
    /// a locked account all get the same 401. Five wrong passwords lock the account for 15 minutes.
    /// </remarks>
    /// <param name="request">The request body.</param>
    /// <param name="loginUser">The login user use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<AuthResponse>> LoginAsync(LoginRequest request, [FromServices] LoginUser loginUser, CancellationToken cancellationToken)
    {
        var session = await loginUser.ExecuteAsync(request.Email, request.Password, ClientIp, cancellationToken);
        if (session is null)
        {
            return Problem(title: LoginFailedMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        return SignedIn(session);
    }

    /// <summary>Swaps the refresh token cookie for a new access token (and a new refresh token).</summary>
    /// <remarks>
    /// Call this when the app loads, and after a 401, to stay signed in. If an old refresh token is used again, the
    /// whole sign-in is ended for safety.
    /// </remarks>
    /// <param name="refreshSession">The refresh session use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<AuthResponse>> RefreshAsync([FromServices] RefreshSession refreshSession, CancellationToken cancellationToken)
    {
        var session = await refreshSession.ExecuteAsync(RefreshCookie.Read(Request), ClientIp, cancellationToken);
        if (session is null)
        {
            RefreshCookie.Clear(Response);
            return Problem(title: "Your session has ended. Please sign in again.", statusCode: StatusCodes.Status401Unauthorized);
        }

        return SignedIn(session);
    }

    /// <summary>Signs out: revokes the refresh token and clears its cookie.</summary>
    /// <param name="logoutUser">The logout user use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> LogoutAsync([FromServices] LogoutUser logoutUser, CancellationToken cancellationToken)
    {
        await logoutUser.ExecuteAsync(CurrentUserId, RefreshCookie.Read(Request), ClientIp, cancellationToken);
        RefreshCookie.Clear(Response);
        return NoContent();
    }

    /// <summary>Emails a password reset link.</summary>
    /// <remarks>Always answers 202, whether or not the email has an account, so it cannot be used to find accounts.</remarks>
    /// <param name="request">The request body.</param>
    /// <param name="requestPasswordReset">The request password reset use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequest request, [FromServices] RequestPasswordReset requestPasswordReset, CancellationToken cancellationToken)
    {
        await requestPasswordReset.ExecuteAsync(request.Email, ClientIp, cancellationToken);
        return Accepted();
    }

    /// <summary>Sets a new password using the email and token from a reset link.</summary>
    /// <remarks>On success the user is signed out everywhere and must sign in with the new password.</remarks>
    /// <param name="request">The request body.</param>
    /// <param name="resetPassword">The reset password use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> ResetPasswordAsync(ResetPasswordRequest request, [FromServices] ResetPassword resetPassword, CancellationToken cancellationToken)
    {
        var result = await resetPassword.ExecuteAsync(request.Email, request.Token, request.NewPassword, ClientIp, cancellationToken);
        if (result.InvalidLink)
        {
            return Problem(title: "This reset link is invalid or has expired. Please ask for a new one.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["newPassword"] = [.. result.PasswordErrors] }));
        }

        return NoContent();
    }

    /// <summary>Who is signed in.</summary>
    /// <param name="getCurrentUser">The get current user use case (supplied by the API).</param>
    /// <param name="cancellationToken">Stops the work if the caller goes away.</param>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<CurrentUserResponse>> GetMeAsync([FromServices] GetCurrentUser getCurrentUser, CancellationToken cancellationToken)
    {
        var user = await getCurrentUser.ExecuteAsync(CurrentUserId, cancellationToken);
        if (user is null)
        {
            return Problem(title: "Your account no longer exists.", statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(ToResponse(user));
    }

    private string ClientIp => ClientIpResolver.Resolve(HttpContext);

    private Guid CurrentUserId => User.GetUserId();

    /// <summary>Sets the refresh cookie and returns the access token.</summary>
    private OkObjectResult SignedIn(AuthSession session)
    {
        RefreshCookie.Write(Response, session.RefreshToken.Value, DateTimeOffset.UtcNow.Add(settings.RefreshTokenLifetime));
        return Ok(new AuthResponse(session.AccessToken.Value, session.AccessToken.ExpiresAt, ToResponse(session.User)));
    }

    private static CurrentUserResponse ToResponse(UserAccount user)
    {
        return new CurrentUserResponse(user.Id, user.Email, user.DisplayName, user.Roles);
    }
}
