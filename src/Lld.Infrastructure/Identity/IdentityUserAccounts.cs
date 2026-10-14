using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lld.Infrastructure.Identity;

/// <summary>
/// Adapter: implements the application's <see cref="IUserAccounts"/> port with ASP.NET Core Identity's
/// <see cref="UserManager{TUser}"/> (password hashing, lockout counters, roles).
/// Note: <c>UserManager</c> saves immediately, outside the use case's unit of work.
/// </summary>
internal sealed class IdentityUserAccounts(UserManager<AppUser> users) : IUserAccounts
{
    public async Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId);
        return user is null ? null : await ToAccountAsync(user);
    }

    public async Task<UserAccount?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email.Value);
        return user is null ? null : await ToAccountAsync(user);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken)
    {
        var all = await users.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var accounts = new List<UserAccount>(all.Count);
        foreach (var user in all)
        {
            accounts.Add(await ToAccountAsync(user));
        }

        return accounts;
    }

    public async Task<Result<UserAccount>> CreateAsync(
        EmailAddress email, string displayName, string role, string temporaryPassword, CancellationToken cancellationToken)
    {
        var user = new AppUser
        {
            UserName = email.Value,
            Email = email.Value,
            EmailConfirmed = true,
            DisplayName = displayName,
            MustChangePassword = true,
        };

        var created = await users.CreateAsync(user, temporaryPassword);
        if (!created.Succeeded)
        {
            return ToError(created);
        }

        var inRole = await users.AddToRoleAsync(user, role);
        return inRole.Succeeded ? await ToAccountAsync(user) : ToError(inRole);
    }

    public async Task<PasswordCheck> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User {userId} not found.");

        if (await users.IsLockedOutAsync(user))
        {
            return PasswordCheck.LockedOut;
        }

        if (await users.CheckPasswordAsync(user, password))
        {
            await users.ResetAccessFailedCountAsync(user);
            return PasswordCheck.Succeeded;
        }

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? PasswordCheck.FailedAndLockedOut : PasswordCheck.Failed;
    }

    public async Task<Result> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return AppErrors.UserNotFound;
        }

        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
        {
            return changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? AppErrors.CurrentPasswordIncorrect
                : ToError(changed);
        }

        user.MustChangePassword = false;
        return ToResult(await users.UpdateAsync(user));
    }

    public async Task<Result> SetTemporaryPasswordAsync(string userId, string temporaryPassword, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return AppErrors.UserNotFound;
        }

        var removed = await users.RemovePasswordAsync(user);
        if (!removed.Succeeded)
        {
            return ToError(removed);
        }

        var added = await users.AddPasswordAsync(user, temporaryPassword);
        if (!added.Succeeded)
        {
            return ToError(added);
        }

        user.MustChangePassword = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        return ToResult(await users.UpdateAsync(user));
    }

    public async Task<Result> DisableAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return AppErrors.UserNotFound;
        }

        user.IsDisabled = true;
        return ToResult(await users.UpdateAsync(user));
    }

    private async Task<UserAccount> ToAccountAsync(AppUser user)
    {
        var roles = await users.GetRolesAsync(user);
        return new UserAccount(
            user.Id,
            EmailAddress.FromTrusted(user.Email!),
            user.DisplayName,
            roles.Order(StringComparer.Ordinal).ToList(),
            user.MustChangePassword,
            user.IsDisabled);
    }

    private static Result ToResult(IdentityResult result) => result.Succeeded ? Result.Success() : ToError(result);

    private static Error ToError(IdentityResult result)
    {
        var first = result.Errors.FirstOrDefault();
        return first is null
            ? new Error("identity.failed", "The account operation failed.")
            : first.Code.StartsWith("validation.", StringComparison.Ordinal)
                ? new Error(first.Code, first.Description)
                : Error.Validation($"password.{first.Code}", first.Description);
    }
}
