using Lld.Domain.Common;

namespace Lld.Domain.Accounts;

// Security events about user accounts and sessions. Use cases raise them; the audit log observes them.

public sealed record UserCreated(string UserId, string Email, string Role, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record UserDisabled(string UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record LoginSucceeded(string UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record LoginFailed(string Email, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record UserLockedOut(string UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record PasswordChanged(string UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record PasswordReset(string UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record SignedOut(string UserId, Guid FamilyId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record SignedOutEverywhere(string UserId, int SessionsRevoked, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record RefreshTokenReuseDetected(string UserId, Guid FamilyId, DateTimeOffset OccurredAt) : IDomainEvent;
