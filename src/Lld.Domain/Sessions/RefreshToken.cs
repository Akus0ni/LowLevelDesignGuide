using Lld.Domain.Common;

namespace Lld.Domain.Sessions;

public enum RevocationReason
{
    Rotated = 0,
    Logout = 1,
    LogoutAllDevices = 2,
    PasswordChanged = 3,
    ReuseDetected = 4,
    UserDisabled = 5,
}

/// <summary>
/// A long-lived credential that buys new access tokens. Only its hash is stored.
/// Every use <see cref="Rotate"/>s it: the old token is revoked and a successor is issued in the same
/// <see cref="FamilyId"/> (one family per sign-in). Presenting a revoked token means it was stolen or
/// replayed, so the whole family is revoked (reuse detection).
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken(Guid id, string userId, string tokenHash, Guid familyId, DateTimeOffset now, TimeSpan lifetime)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAt = now;
        ExpiresAt = now + lifetime;
    }

    // Required by EF Core.
    private RefreshToken()
    {
        UserId = null!;
        TokenHash = null!;
    }

    public Guid Id { get; private set; }

    public string UserId { get; private set; }

    public string TokenHash { get; private set; }

    public Guid FamilyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RevocationReason? RevokedReason { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Factory: the first token of a new sign-in starts a new family.</summary>
    public static RefreshToken Issue(string userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        new(Guid.NewGuid(), userId, tokenHash, Guid.NewGuid(), now, lifetime);

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    /// <summary>Revokes this token and returns its successor in the same family.</summary>
    public Result<RefreshToken> Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (!IsActive(now))
        {
            return new Error("auth.refresh_token_inactive", "The session has expired. Please sign in again.");
        }

        var successor = new RefreshToken(Guid.NewGuid(), UserId, newTokenHash, FamilyId, now, lifetime);
        Revoke(now, RevocationReason.Rotated);
        ReplacedByTokenId = successor.Id;
        return successor;
    }

    /// <summary>Idempotent: revoking an already revoked token keeps the original reason.</summary>
    public void Revoke(DateTimeOffset now, RevocationReason reason)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;
    }
}
