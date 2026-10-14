using Lld.Domain.Access;
using Lld.Domain.Audit;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Domain.Sessions;

namespace Lld.Application.Abstractions;

// Ports: what the use cases need from the outside world. Infrastructure provides the implementations,
// so the application layer depends only on these interfaces (Dependency Inversion).

public interface IAccessRequestRepository
{
    Task<AccessRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessRequest?> FindPendingByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccessRequest>> ListAsync(AccessRequestStatus? status, CancellationToken cancellationToken);

    void Add(AccessRequest request);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ListActiveByFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(string userId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(RefreshToken token);
}

public interface IAuditLog
{
    void Append(AuditEntry entry);

    Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int take, CancellationToken cancellationToken);
}

/// <summary>Commits all changes of one use case together, after dispatching its domain events.</summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>Collects events that do not belong to an aggregate (e.g. a failed login).</summary>
public interface IDomainEvents
{
    void Raise(IDomainEvent domainEvent);
}

/// <summary>Observer: reacts to domain events during commit (e.g. the audit trail).</summary>
public interface IDomainEventObserver
{
    void Handle(IDomainEvent domainEvent);
}

public interface ITokenIssuer
{
    AccessToken Issue(UserAccount user, DateTimeOffset now);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Creates random refresh tokens and hashes them; only hashes are ever stored.</summary>
public interface IRefreshTokenCodec
{
    NewRefreshToken Generate();

    string Hash(string rawToken);
}

public sealed record NewRefreshToken(string RawValue, string Hash);

/// <summary>Strategy: the rule a user-chosen password must satisfy.</summary>
public interface IPasswordPolicy
{
    Result Validate(string password, EmailAddress email);
}

/// <summary>Strategy: how one-time passwords for new or reset accounts are generated.</summary>
public interface ITemporaryPasswordGenerator
{
    string Generate();
}
