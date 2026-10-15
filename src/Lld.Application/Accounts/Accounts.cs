using Lld.Application.Abstractions;
using Lld.Application.Auth;
using Lld.Application.Common;
using Lld.Domain.Accounts;
using Lld.Domain.Audit;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Domain.Sessions;

namespace Lld.Application.Accounts;

/// <summary>A temporary password to hand to a person. Shown once, never stored in clear text.</summary>
public sealed record IssuedCredentials(string Email, string TemporaryPassword);

/// <summary>Creates accounts with a generated temporary password (shared by approve and create-user).</summary>
public sealed class AccountProvisioner(
    IUserAccounts accounts,
    ITemporaryPasswordGenerator passwords,
    IDomainEvents events,
    TimeProvider time)
{
    public async Task<Result<IssuedCredentials>> CreateAsync(
        EmailAddress email, string displayName, string role, CancellationToken cancellationToken)
    {
        if (!Roles.IsKnown(role))
        {
            return AppErrors.UnknownRole;
        }

        if (await accounts.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return AppErrors.UserAlreadyExists;
        }

        var temporaryPassword = passwords.Generate();
        var created = await accounts.CreateAsync(email, displayName, role, temporaryPassword, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        events.Raise(new UserCreated(created.Value.Id, email.Value, role, time.GetUtcNow()));
        return new IssuedCredentials(email.Value, temporaryPassword);
    }
}

public sealed record CreateUserCommand(string Email, string? DisplayName, string Role);

public sealed class CreateUser(AccountProvisioner provisioner, IUnitOfWork unitOfWork)
{
    public async Task<Result<IssuedCredentials>> ExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error!;
        }

        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? email.Value.Value : command.DisplayName.Trim();
        var created = await provisioner.CreateAsync(email.Value, displayName, command.Role, cancellationToken);
        if (created.IsSuccess)
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return created;
    }
}

/// <summary>Admin reset: new temporary password, must change on next sign-in, all sessions ended.</summary>
public sealed class ResetPassword(
    IUserAccounts accounts,
    ITemporaryPasswordGenerator passwords,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<IssuedCredentials>> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        var user = await FindAsync(accounts, email, cancellationToken);
        if (user.IsFailure)
        {
            return user.Error!;
        }

        var temporaryPassword = passwords.Generate();
        var reset = await accounts.SetTemporaryPasswordAsync(user.Value.Id, temporaryPassword, cancellationToken);
        if (reset.IsFailure)
        {
            return reset.Error!;
        }

        var now = time.GetUtcNow();
        await sessions.RevokeAllAsync(user.Value.Id, RevocationReason.PasswordChanged, now, cancellationToken);
        events.Raise(new PasswordReset(user.Value.Id, user.Value.Email.Value, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return new IssuedCredentials(user.Value.Email.Value, temporaryPassword);
    }

    internal static async Task<Result<UserAccount>> FindAsync(IUserAccounts accounts, string email, CancellationToken cancellationToken)
    {
        var address = EmailAddress.Create(email);
        if (address.IsFailure)
        {
            return address.Error!;
        }

        var user = await accounts.FindByEmailAsync(address.Value, cancellationToken);
        return user is null ? AppErrors.UserNotFound : user;
    }
}

public sealed class DisableUser(
    IUserAccounts accounts,
    SessionManager sessions,
    IDomainEvents events,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        var user = await ResetPassword.FindAsync(accounts, email, cancellationToken);
        if (user.IsFailure)
        {
            return user.Error!;
        }

        var disabled = await accounts.DisableAsync(user.Value.Id, cancellationToken);
        if (disabled.IsFailure)
        {
            return disabled;
        }

        var now = time.GetUtcNow();
        await sessions.RevokeAllAsync(user.Value.Id, RevocationReason.UserDisabled, now, cancellationToken);
        events.Raise(new UserDisabled(user.Value.Id, user.Value.Email.Value, now));
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ListUsers(IUserAccounts accounts)
{
    public Task<IReadOnlyList<UserAccount>> ExecuteAsync(CancellationToken cancellationToken) => accounts.ListAsync(cancellationToken);
}

public sealed class ListAuditEntries(IAuditLog auditLog)
{
    public const int DefaultTake = 50;

    public Task<IReadOnlyList<AuditEntry>> ExecuteAsync(int? take, CancellationToken cancellationToken) =>
        auditLog.ListRecentAsync(Math.Clamp(take ?? DefaultTake, 1, 1000), cancellationToken);
}
