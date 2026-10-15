using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Accounts;
using Lld.Domain.Common;
using Lld.Domain.Sessions;

namespace Lld.Application.Auth;

public sealed record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword);

/// <summary>
/// Changes the password (also used for the forced change after a temporary password), ends every
/// existing session, and starts a fresh one so the caller stays signed in on this device only.
/// </summary>
public sealed class ChangePassword(
    IUserAccounts accounts,
    IPasswordPolicy passwordPolicy,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<SignInResult>> ExecuteAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await accounts.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null || user.IsDisabled)
        {
            return AppErrors.SessionInvalid;
        }

        var policy = passwordPolicy.Validate(command.NewPassword, user.Email);
        if (policy.IsFailure)
        {
            return policy.Error!;
        }

        var changed = await accounts.ChangePasswordAsync(user.Id, command.CurrentPassword, command.NewPassword, cancellationToken);
        if (changed.IsFailure)
        {
            return changed.Error!;
        }

        var now = time.GetUtcNow();
        await sessions.RevokeAllAsync(user.Id, RevocationReason.PasswordChanged, now, cancellationToken);
        var refreshed = await accounts.FindByIdAsync(user.Id, cancellationToken) ?? user;
        var session = sessions.Start(refreshed, now);
        events.Raise(new PasswordChanged(user.Id, user.Email.Value, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return session;
    }
}

/// <summary>Who am I? Used by clients to restore state and to check the must-change flag.</summary>
public sealed class GetCurrentUser(IUserAccounts accounts)
{
    public async Task<Result<UserAccount>> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await accounts.FindByIdAsync(userId, cancellationToken);
        return user is null || user.IsDisabled ? AppErrors.SessionInvalid : user;
    }
}
