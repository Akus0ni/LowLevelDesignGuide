using Lld.Application.Abstractions;
using Lld.Domain.Accounts;
using Lld.Domain.Common;
using Lld.Domain.Sessions;

namespace Lld.Application.Auth;

/// <summary>Ends the session the refresh token belongs to. Always succeeds (idempotent).</summary>
public sealed class Logout(
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenCodec codec,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result> ExecuteAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Success();
        }

        var token = await refreshTokens.FindByHashAsync(codec.Hash(rawRefreshToken), cancellationToken);
        if (token is null)
        {
            return Result.Success();
        }

        var now = time.GetUtcNow();
        var revoked = await sessions.RevokeFamilyAsync(token.FamilyId, RevocationReason.Logout, now, cancellationToken);
        if (revoked > 0)
        {
            events.Raise(new SignedOut(token.UserId, token.FamilyId, now));
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return Result.Success();
    }
}

/// <summary>Ends every session of the signed-in user, on every device.</summary>
public sealed class LogoutAllDevices(
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<int>> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var revoked = await sessions.RevokeAllAsync(userId, RevocationReason.LogoutAllDevices, now, cancellationToken);
        events.Raise(new SignedOutEverywhere(userId, revoked, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return revoked;
    }
}
