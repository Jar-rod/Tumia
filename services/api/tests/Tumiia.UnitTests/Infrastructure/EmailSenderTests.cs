using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tumiia.Domain.Email;
using Tumiia.Infrastructure.Email;

namespace Tumiia.UnitTests.Infrastructure;

public class EmailSenderTests
{
    private static readonly EmailMessage Message = new("alice@example.test", "Reset your password", "Open https://app/reset?token=secret");

    [Fact]
    public async Task SesEmailSender_WhenSending_UsesTheConfiguredFromAddressAndPlainText()
    {
        var ses = Substitute.For<IAmazonSimpleEmailServiceV2>();
        var sender = new SesEmailSender(ses, Options.Create(new EmailSettings { FromAddress = "no-reply@app.example.test" }), NullLogger<SesEmailSender>.Instance);

        await sender.SendAsync(Message, CancellationToken.None);

        await ses.Received().SendEmailAsync(
            Arg.Is<SendEmailRequest>(request => request.FromEmailAddress == "no-reply@app.example.test"
                && request.Destination.ToAddresses.Single() == "alice@example.test"
                && request.Content.Simple.Subject.Data == "Reset your password"
                && request.Content.Simple.Body.Text.Data == Message.Body),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoggingEmailSender_WhenSending_WritesTheWholeEmailToAFileInsteadOfSending()
    {
        var contentRoot = Directory.CreateTempSubdirectory("tumiia-email-test").FullName;
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRoot);

        await new LoggingEmailSender(environment, NullLogger<LoggingEmailSender>.Instance).SendAsync(Message, CancellationToken.None);

        var file = Directory.GetFiles(Path.Combine(contentRoot, LoggingEmailSender.FolderName)).ShouldHaveSingleItem();
        (await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken)).ShouldContain("https://app/reset?token=secret");
        Directory.Delete(contentRoot, recursive: true);
    }

    [Theory]
    [InlineData("Ses", true)]
    [InlineData("ses", true)]
    [InlineData("Logging", false)]
    public void EmailSettings_WhenProviderIsSet_ChoosesSesOnlyForSes(string provider, bool expected)
    {
        var settings = new EmailSettings { Provider = provider };

        settings.UsesSes.ShouldBe(expected);
    }
}
