using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Accounts;
using Lld.Domain.Common;
using Lld.Domain.Identity;

namespace Lld.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

/// <summary>Email + password → a new session. Every outcome is audited.</summary>
public sealed class Login(
    IUserAccounts accounts,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<SignInResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return AppErrors.InvalidCredentials;
        }

        var user = await accounts.FindByEmailAsync(email.Value, cancellationToken);
        if (user is null || user.IsDisabled)
        {
            return await FailAsync(email.Value, user is null ? "unknown email" : "account disabled", now, cancellationToken);
        }

        var check = await accounts.CheckPasswordAsync(user.Id, command.Password, cancellationToken);
        switch (check)
        {
            case PasswordCheck.LockedOut:
                return await FailAsync(email.Value, "locked out", now, cancellationToken, AppErrors.LockedOut);
            case PasswordCheck.FailedAndLockedOut:
                events.Raise(new UserLockedOut(user.Id, user.Email.Value, now));
                return await FailAsync(email.Value, "wrong password", now, cancellationToken, AppErrors.LockedOut);
            case PasswordCheck.Failed:
                return await FailAsync(email.Value, "wrong password", now, cancellationToken);
        }

        var session = sessions.Start(user, now);
        events.Raise(new LoginSucceeded(user.Id, user.Email.Value, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return session;
    }

    private async Task<Result<SignInResult>> FailAsync(
        EmailAddress email, string reason, DateTimeOffset now, CancellationToken cancellationToken, Error? error = null)
    {
        events.Raise(new LoginFailed(email.Value, reason, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return error ?? AppErrors.InvalidCredentials;
    }
}
