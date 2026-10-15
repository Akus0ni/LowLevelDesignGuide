using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Access;
using Lld.Domain.Audit;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Lld.Infrastructure.Persistence;

internal sealed class EfAccessRequestRepository(LldDbContext db) : IAccessRequestRepository
{
    public Task<AccessRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.AccessRequests.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<AccessRequest?> FindPendingByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        db.AccessRequests.SingleOrDefaultAsync(r => r.Email == email && r.Status == AccessRequestStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<AccessRequest>> ListAsync(AccessRequestStatus? status, CancellationToken cancellationToken) =>
        await db.AccessRequests
            .Where(r => status == null || r.Status == status)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(AccessRequest request) => db.AccessRequests.Add(request);
}

internal sealed class EfRefreshTokenRepository(LldDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(string userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}

internal sealed class EfAuditLog(LldDbContext db) : IAuditLog
{
    public void Append(AuditEntry entry) => db.AuditEntries.Add(entry);

    public async Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int take, CancellationToken cancellationToken) =>
        await db.AuditEntries.OrderByDescending(e => e.OccurredAt).Take(take).ToListAsync(cancellationToken);
}

/// <summary>
/// Unit of Work: collects domain events from changed aggregates and the event collector, lets the observers
/// react (e.g. the audit trail adds rows), and then saves everything in one <c>SaveChanges</c> call.
/// </summary>
internal sealed class EfUnitOfWork(
    LldDbContext db, DomainEventCollector collector, DomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var aggregates = db.ChangeTracker.Entries<AggregateRoot>().Select(e => e.Entity).ToList();
        var events = aggregates.SelectMany(a => a.DomainEvents).Concat(collector.Drain()).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        dispatcher.Dispatch(events);
        await db.SaveChangesAsync(cancellationToken);
    }
}
