using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.UnitTests.Infrastructure;

public class PasswordPolicyTests
{
    private static async Task<IdentityResult> ValidateAsync(string password)
    {
        var options = new IdentityOptions();
        IdentityRules.Configure(options);
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), Options.Create(options), null, null, null, null, null, null, null);

        return await new PasswordValidator<ApplicationUser>().ValidateAsync(userManager, new ApplicationUser(), password);
    }

    [Fact]
    public async Task Validate_WhenShorterThanTenCharacters_Rejects()
    {
        var result = await ValidateAsync("Short1!a");

        result.Errors.ShouldContain(error => error.Code == "PasswordTooShort");
    }

    [Theory]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!!")]
    [InlineData("NoSymbols12345")]
    public async Task Validate_WhenMissingACharacterType_Rejects(string password)
    {
        var result = await ValidateAsync(password);

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Validate_WhenTenCharactersOfEveryType_Accepts()
    {
        var result = await ValidateAsync("Bricks-R18k");

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Configure_WhenApplied_LocksForFifteenMinutesAfterFiveFailures()
    {
        var options = new IdentityOptions();

        IdentityRules.Configure(options);

        options.Lockout.MaxFailedAccessAttempts.ShouldBe(5);
        options.Lockout.DefaultLockoutTimeSpan.ShouldBe(TimeSpan.FromMinutes(15));
        options.Lockout.AllowedForNewUsers.ShouldBeTrue();
        options.User.RequireUniqueEmail.ShouldBeTrue();
    }
}
