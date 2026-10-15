using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Accounts;
using Lld.Domain.Common;
using Lld.Domain.Sessions;

namespace Lld.Application.Auth;

/// <summary>
/// Trades a refresh token for a new session (rotation). If the presented token was already
/// rotated away, someone is replaying it: the whole family is revoked (reuse detection).
/// </summary>
public sealed class RefreshSession(
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenCodec codec,
    IUserAccounts accounts,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<SignInResult>> ExecuteAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return AppErrors.SessionInvalid;
        }

        var now = time.GetUtcNow();
        var token = await refreshTokens.FindByHashAsync(codec.Hash(rawRefreshToken), cancellationToken);
        if (token is null)
        {
            return AppErrors.SessionInvalid;
        }

        if (token.RevokedReason == RevocationReason.Rotated)
        {
            await sessions.RevokeFamilyAsync(token.FamilyId, RevocationReason.ReuseDetected, now, cancellationToken);
            events.Raise(new RefreshTokenReuseDetected(token.UserId, token.FamilyId, now));
            await unitOfWork.CommitAsync(cancellationToken);
            return AppErrors.RefreshTokenReused;
        }

        if (!token.IsActive(now))
        {
            return AppErrors.SessionInvalid;
        }

        var user = await accounts.FindByIdAsync(token.UserId, cancellationToken);
        if (user is null || user.IsDisabled)
        {
            await sessions.RevokeFamilyAsync(token.FamilyId, RevocationReason.UserDisabled, now, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return AppErrors.SessionInvalid;
        }

        var session = sessions.Continue(user, token, now);
        if (session.IsSuccess)
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return session;
    }
}
