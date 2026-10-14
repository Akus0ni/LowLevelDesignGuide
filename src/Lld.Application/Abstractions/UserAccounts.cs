using Lld.Domain.Common;
using Lld.Domain.Identity;

namespace Lld.Application.Abstractions;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Learner = "Learner";

    public static readonly IReadOnlyList<string> All = [Admin, Learner];

    public static bool IsKnown(string role) => All.Contains(role, StringComparer.Ordinal);
}

/// <summary>A read model of an account, independent of the identity framework behind it.</summary>
public sealed record UserAccount(
    string Id,
    EmailAddress Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    bool IsDisabled);

public enum PasswordCheck
{
    Succeeded,
    Failed,
    LockedOut,

    /// <summary>The password was wrong and this attempt triggered the lockout.</summary>
    FailedAndLockedOut,
}

/// <summary>
/// Adapter port over the identity framework (ASP.NET Core Identity in Infrastructure), so use cases
/// talk about accounts, not about <c>UserManager</c>.
/// </summary>
public interface IUserAccounts
{
    Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken);

    Task<UserAccount?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Creates an account that must change <paramref name="temporaryPassword"/> on first sign-in.</summary>
    Task<Result<UserAccount>> CreateAsync(
        EmailAddress email, string displayName, string role, string temporaryPassword, CancellationToken cancellationToken);

    /// <summary>Checks the password and updates the failed-attempt / lockout counters.</summary>
    Task<PasswordCheck> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken);

    /// <summary>Changes the password and clears the must-change flag.</summary>
    Task<Result> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>Replaces the password with a temporary one and sets the must-change flag.</summary>
    Task<Result> SetTemporaryPasswordAsync(string userId, string temporaryPassword, CancellationToken cancellationToken);

    Task<Result> DisableAsync(string userId, CancellationToken cancellationToken);
}
