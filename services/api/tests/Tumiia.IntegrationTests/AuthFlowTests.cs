using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tumiia.Api.Dtos;
using Tumiia.Domain.Audit;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthFlowTests(TumiiaApiFactory factory)
{
    private readonly AuthClient _auth = new(factory.CreateHttpsClient());

    private async Task<List<AuditEventType>> AuditEventsForAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>();
        return await database.AuditEvents.Where(auditEvent => auditEvent.UserId == userId)
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .Select(auditEvent => auditEvent.EventType)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Register_Login_Me_Refresh_Logout_WorksEndToEnd()
    {
        var email = AuthClient.NewEmail();

        var registered = await _auth.RegisterAsync(email, displayName: "Thandi");
        var login = await _auth.LoginAsync(email);
        var session = await AuthClient.ReadSessionAsync(login);
        var me = await _auth.MeAsync(session.AccessToken);
        var refreshed = await _auth.RefreshAsync(AuthClient.RefreshCookieFrom(login));
        var newSession = await AuthClient.ReadSessionAsync(refreshed);
        var newCookie = AuthClient.RefreshCookieFrom(refreshed);
        var logout = await _auth.LogoutAsync(newSession.AccessToken, newCookie);
        var refreshAfterLogout = await _auth.RefreshAsync(newCookie);

        registered.StatusCode.ShouldBe(HttpStatusCode.OK);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = (await me.Content.ReadFromJsonAsync<CurrentUserResponse>(TestContext.Current.CancellationToken))!;
        user.Email.ShouldBe(email);
        user.DisplayName.ShouldBe("Thandi");
        user.Roles.ShouldBe(["User"]);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        newCookie.ShouldNotBe(AuthClient.RefreshCookieFrom(login));
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        AuthClient.RefreshCookieFrom(logout).ShouldBe(string.Empty);
        refreshAfterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuditEventsForAsync(user.Id)).ShouldBe([AuditEventType.Registered, AuditEventType.LoginSucceeded, AuditEventType.LoggedOut]);
    }

    [Fact]
    public async Task Register_WhenSuccessful_SetsAStrictHttpOnlyRefreshCookie()
    {
        var response = await _auth.RegisterAsync(AuthClient.NewEmail());

        var cookie = response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant();

        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/v1/auth");
    }

    [Fact]
    public async Task Refresh_WhenAnOldTokenIsReused_EndsTheWholeSignIn()
    {
        var (_, firstCookie, _) = await _auth.RegisterNewUserAsync();
        var rotated = await _auth.RefreshAsync(firstCookie);
        var currentCookie = AuthClient.RefreshCookieFrom(rotated);

        var replay = await _auth.RefreshAsync(firstCookie);
        var currentAfterReplay = await _auth.RefreshAsync(currentCookie);

        rotated.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        currentAfterReplay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WhenEmailIsTaken_Answers409()
    {
        var email = AuthClient.NewEmail();
        await _auth.RegisterAsync(email);

        var again = await _auth.RegisterAsync(email);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WhenPasswordIsWeak_Answers400WithThePasswordProblems()
    {
        var response = await _auth.RegisterAsync(AuthClient.NewEmail(), password: "short");

        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken))!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.Errors.ShouldContainKey("password");
    }

    [Fact]
    public async Task Login_WhenEmailOrPasswordIsWrong_GivesTheSameAnswerForBoth()
    {
        var (_, _, email) = await _auth.RegisterNewUserAsync();

        var wrongPassword = await _auth.LoginAsync(email, "Wrong-Password1");
        var unknownEmail = await _auth.LoginAsync(AuthClient.NewEmail());

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var first = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        var second = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        first!.Title.ShouldBe("Email or password is incorrect.");
        second!.Title.ShouldBe(first.Title);
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_LocksTheAccountEvenForTheRightPassword()
    {
        var (session, _, email) = await _auth.RegisterNewUserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await _auth.LoginAsync(email, "Wrong-Password1");
        }

        var rightPassword = await _auth.LoginAsync(email);

        rightPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await rightPassword.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problem!.Title.ShouldBe("Email or password is incorrect.");
        (await AuditEventsForAsync(session.User.Id)).ShouldContain(AuditEventType.LockedOut);
    }

    [Fact]
    public async Task Me_WithoutAToken_Answers401ProblemDetails()
    {
        var response = await _auth.MeAsync(accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Register_WhenEmailIsAConfiguredAdmin_GivesTheAdminRole()
    {
        var response = await _auth.RegisterAsync(TumiiaApiFactory.AdminEmail);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            response = await _auth.LoginAsync(TumiiaApiFactory.AdminEmail);
        }

        var session = await AuthClient.ReadSessionAsync(response);

        session.User.Roles.ShouldBe(["User", "Admin"], ignoreOrder: true);
    }
}
