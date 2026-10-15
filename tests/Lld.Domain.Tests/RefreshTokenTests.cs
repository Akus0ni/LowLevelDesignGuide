using FluentAssertions;
using Lld.Domain.Sessions;

namespace Lld.Domain.Tests;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Issue_StartsANewFamily()
    {
        var first = RefreshToken.Issue("user-1", "hash-1", Now, Lifetime);
        var second = RefreshToken.Issue("user-1", "hash-2", Now, Lifetime);

        first.FamilyId.Should().NotBe(second.FamilyId);
        first.ExpiresAt.Should().Be(Now + Lifetime);
        first.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void Rotate_RevokesCurrentAndIssuesSuccessorInSameFamily()
    {
        var current = RefreshToken.Issue("user-1", "hash-1", Now, Lifetime);
        var later = Now.AddDays(1);

        var successor = current.Rotate("hash-2", later, Lifetime).Value;

        successor.FamilyId.Should().Be(current.FamilyId);
        successor.UserId.Should().Be("user-1");
        successor.ExpiresAt.Should().Be(later + Lifetime);
        current.IsRevoked.Should().BeTrue();
        current.RevokedReason.Should().Be(RevocationReason.Rotated);
        current.ReplacedByTokenId.Should().Be(successor.Id);
    }

    [Fact]
    public void Rotate_FailsForRevokedToken()
    {
        var token = RefreshToken.Issue("user-1", "hash-1", Now, Lifetime);
        token.Revoke(Now, RevocationReason.Logout);

        token.Rotate("hash-2", Now, Lifetime).Error!.Code.Should().Be("auth.refresh_token_inactive");
    }

    [Fact]
    public void Rotate_FailsForExpiredToken()
    {
        var token = RefreshToken.Issue("user-1", "hash-1", Now, Lifetime);

        token.Rotate("hash-2", Now + Lifetime, Lifetime).IsFailure.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Revoke_IsIdempotentAndKeepsFirstReason()
    {
        var token = RefreshToken.Issue("user-1", "hash-1", Now, Lifetime);

        token.Revoke(Now, RevocationReason.Logout);
        token.Revoke(Now.AddMinutes(1), RevocationReason.ReuseDetected);

        token.RevokedReason.Should().Be(RevocationReason.Logout);
        token.RevokedAt.Should().Be(Now);
    }
}
