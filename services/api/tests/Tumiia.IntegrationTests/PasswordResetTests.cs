using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PasswordResetTests(TumiiaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateHttpsClient();

    private Task<HttpResponseMessage> ForgotAsync(string email)
    {
        return _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email }, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> ResetAsync(string email, string token, string newPassword)
    {
        return _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token, newPassword }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailIsUnknown_StillAnswers202AndSendsNothing()
    {
        var email = AuthClient.NewEmail();

        var response = await ForgotAsync(email);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        factory.Emails.Sent.ShouldNotContain(sent => sent.To == email);
    }

    [Fact]
    public async Task ResetPassword_WithTheEmailedLink_ChangesThePasswordAndRevokesOldSessions()
    {
        var auth = new AuthClient(_client);
        var (_, oldCookie, email) = await auth.RegisterNewUserAsync();
        await ForgotAsync(email);
        var link = factory.Emails.Sent.Last(sent => sent.To == email).Body.Split('\n').Single(line => line.Contains("/reset-password?"));
        var query = HttpUtility.ParseQueryString(new Uri(link.Trim()).Query);

        var reset = await ResetAsync(query["email"]!, query["token"]!, "New-Bricks-99");
        var oldSessionRefresh = await auth.RefreshAsync(oldCookie);
        var oldPassword = await auth.LoginAsync(email);
        var newPassword = await auth.LoginAsync(email, "New-Bricks-99");

        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        oldSessionRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newPassword.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithAMadeUpToken_Answers400()
    {
        var (_, _, email) = await new AuthClient(_client).RegisterNewUserAsync();

        var response = await ResetAsync(email, "made-up-token", "New-Bricks-99");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
