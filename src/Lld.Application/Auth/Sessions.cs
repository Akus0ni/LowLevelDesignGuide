using Lld.Application.Abstractions;
using Lld.Domain.Sessions;

namespace Lld.Application.Auth;

public sealed class AuthOptions
{
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>What a successful sign-in, refresh or password change hands back to the client.</summary>
public sealed record SignInResult(UserAccount User, AccessToken AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

/// <summary>Issues and revokes sessions (an access token plus a refresh-token family).</summary>
public sealed class SessionManager(
    ITokenIssuer tokenIssuer,
    IRefreshTokenCodec codec,
    IRefreshTokenRepository refreshTokens,
    AuthOptions options)
{
    /// <summary>Starts a new session: a fresh refresh-token family and an access token.</summary>
    public SignInResult Start(UserAccount user, DateTimeOffset now)
    {
        var secret = codec.Generate();
        var token = RefreshToken.Issue(user.Id, secret.Hash, now, options.RefreshTokenLifetime);
        refreshTokens.Add(token);
        return new SignInResult(user, tokenIssuer.Issue(user, now), secret.RawValue, token.ExpiresAt);
    }

    /// <summary>Continues an existing session by rotating its refresh token.</summary>
    public Domain.Common.Result<SignInResult> Continue(UserAccount user, RefreshToken current, DateTimeOffset now)
    {
        var secret = codec.Generate();
        var rotated = current.Rotate(secret.Hash, now, options.RefreshTokenLifetime);
        if (rotated.IsFailure)
        {
            return rotated.Error!;
        }

        refreshTokens.Add(rotated.Value);
        return new SignInResult(user, tokenIssuer.Issue(user, now), secret.RawValue, rotated.Value.ExpiresAt);
    }

    public async Task<int> RevokeFamilyAsync(Guid familyId, RevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await refreshTokens.ListActiveByFamilyAsync(familyId, now, cancellationToken);
        Revoke(active, reason, now);
        return active.Count;
    }

    /// <summary>Revokes every active session of the user; returns how many families were ended.</summary>
    public async Task<int> RevokeAllAsync(string userId, RevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await refreshTokens.ListActiveByUserAsync(userId, now, cancellationToken);
        Revoke(active, reason, now);
        return active.Select(t => t.FamilyId).Distinct().Count();
    }

    private static void Revoke(IEnumerable<RefreshToken> tokens, RevocationReason reason, DateTimeOffset now)
    {
        foreach (var token in tokens)
        {
            token.Revoke(now, reason);
        }
    }
}
