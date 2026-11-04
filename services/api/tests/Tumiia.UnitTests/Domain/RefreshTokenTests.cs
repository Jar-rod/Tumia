using Tumiia.Domain.Auth;

namespace Tumiia.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    [Fact]
    public void StartFamily_WhenUserSignsIn_CreatesAnActiveTokenInANewFamily()
    {
        var userId = Guid.NewGuid();

        var token = RefreshToken.StartFamily(userId, "hash-1", Now, Lifetime);

        token.UserId.ShouldBe(userId);
        token.FamilyId.ShouldNotBe(Guid.Empty);
        token.ExpiresAt.ShouldBe(Now.Add(Lifetime));
        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void Rotate_WhenActive_RevokesItAndLinksAReplacementInTheSameFamily()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);

        var replacement = token.Rotate("hash-2", Now.AddMinutes(5), Lifetime);

        token.IsActive(Now.AddMinutes(5)).ShouldBeFalse();
        token.ReplacedByTokenId.ShouldBe(replacement.Id);
        replacement.FamilyId.ShouldBe(token.FamilyId);
        replacement.TokenHash.ShouldBe("hash-2");
        replacement.IsActive(Now.AddMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    public void Rotate_WhenAlreadyUsed_IsRefused()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);
        token.Rotate("hash-2", Now, Lifetime);

        var rotateAgain = () => token.Rotate("hash-3", Now, Lifetime);

        rotateAgain.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void IsReuse_WhenTokenWasAlreadyRotated_IsTrue()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);
        token.Rotate("hash-2", Now, Lifetime);

        var reused = token.IsReuse();

        reused.ShouldBeTrue();
    }

    [Fact]
    public void IsReuse_WhenTokenWasRevokedByLogout_IsFalse()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);
        token.Revoke(Now);

        var reused = token.IsReuse();

        reused.ShouldBeFalse();
        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_IsFalseButItIsNotReuse()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);

        var active = token.IsActive(Now.Add(Lifetime));

        active.ShouldBeFalse();
        token.IsReuse().ShouldBeFalse();
    }

    [Fact]
    public void Revoke_WhenCalledTwice_KeepsTheFirstTime()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        token.RevokedAt.ShouldBe(Now);
    }
}
