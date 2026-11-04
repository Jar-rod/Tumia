using Tumiia.Domain.Auth;

namespace Tumiia.UnitTests.Domain;

public class RefreshTokenSecretTests
{
    [Fact]
    public void Generate_WhenCalled_Makes256RandomBitsAndItsSha256()
    {
        var secret = RefreshTokenSecret.Generate();

        secret.Value.Length.ShouldBe(43);
        secret.Hash.Length.ShouldBe(64);
        secret.Hash.ShouldBe(RefreshTokenSecret.HashOf(secret.Value));
    }

    [Fact]
    public void Generate_WhenCalledTwice_GivesDifferentTokens()
    {
        var first = RefreshTokenSecret.Generate();

        var second = RefreshTokenSecret.Generate();

        second.Value.ShouldNotBe(first.Value);
    }

    [Fact]
    public void Generate_WhenCalled_IsSafeToPutInACookie()
    {
        var secret = RefreshTokenSecret.Generate();

        var cookieSafe = secret.Value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

        cookieSafe.ShouldBeTrue();
    }
}
