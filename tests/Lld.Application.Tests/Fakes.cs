using Lld.Application.Abstractions;
using Lld.Application.Common;
using Lld.Domain.Access;
using Lld.Domain.Audit;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Domain.Sessions;
using Microsoft.Extensions.Time.Testing;

namespace Lld.Application.Tests;

internal sealed class InMemoryAccessRequests : IAccessRequestRepository
{
    public List<AccessRequest> Items { get; } = [];

    public Task<AccessRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(r => r.Id == id));

    public Task<AccessRequest?> FindPendingByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(r => r.Email == email && r.Status == AccessRequestStatus.Pending));

    public Task<IReadOnlyList<AccessRequest>> ListAsync(AccessRequestStatus? status, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AccessRequest>>(Items.Where(r => status is null || r.Status == status).ToList());

    public void Add(AccessRequest request) => Items.Add(request);
}

internal sealed class InMemoryRefreshTokens : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> ListActiveByFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Items.Where(t => t.FamilyId == familyId && t.IsActive(now)).ToList());

    public Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(string userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Items.Where(t => t.UserId == userId && t.IsActive(now)).ToList());

    public void Add(RefreshToken token) => Items.Add(token);
}

internal sealed class InMemoryAuditLog : IAuditLog
{
    public List<AuditEntry> Items { get; } = [];

    public IEnumerable<AuditEventType> Types => Items.Select(e => e.Type);

    public void Append(AuditEntry entry) => Items.Add(entry);

    public Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AuditEntry>>(Items.OrderByDescending(e => e.OccurredAt).Take(take).ToList());
}

/// <summary>Mimics the real unit of work: drains aggregate + collected events, dispatches them, "saves".</summary>
internal sealed class FakeUnitOfWork(
    InMemoryAccessRequests requests, DomainEventCollector collector, DomainEventDispatcher dispatcher) : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        var events = requests.Items.SelectMany(r => r.DomainEvents).Concat(collector.Drain()).ToList();
        requests.Items.ForEach(r => r.ClearDomainEvents());
        dispatcher.Dispatch(events);
        Commits++;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory accounts with Identity-like lockout: 5 failures lock for 15 minutes.</summary>
internal sealed class FakeUserAccounts(FakeTimeProvider time) : IUserAccounts
{
    private readonly Dictionary<string, Account> _byId = [];

    public Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(userId, out var a) ? a.ToModel() : null);

    public Task<UserAccount?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.SingleOrDefault(a => a.Email == email)?.ToModel());

    public Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(_byId.Values.Select(a => a.ToModel()).ToList());

    public Task<Result<UserAccount>> CreateAsync(
        EmailAddress email, string displayName, string role, string temporaryPassword, CancellationToken cancellationToken)
    {
        var account = new Account($"user-{_byId.Count + 1}", email, displayName, role) { Password = temporaryPassword, MustChange = true };
        _byId[account.Id] = account;
        return Task.FromResult(Result.Success(account.ToModel()));
    }

    public Task<PasswordCheck> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var account = _byId[userId];
        if (account.LockedUntil > time.GetUtcNow())
        {
            return Task.FromResult(PasswordCheck.LockedOut);
        }

        if (account.Password == password)
        {
            account.Failures = 0;
            return Task.FromResult(PasswordCheck.Succeeded);
        }

        if (++account.Failures < 5)
        {
            return Task.FromResult(PasswordCheck.Failed);
        }

        account.Failures = 0;
        account.LockedUntil = time.GetUtcNow().AddMinutes(15);
        return Task.FromResult(PasswordCheck.FailedAndLockedOut);
    }

    public Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var account = _byId[userId];
        if (account.Password != currentPassword)
        {
            return Task.FromResult(Result.Failure(AppErrors.CurrentPasswordIncorrect));
        }

        account.Password = newPassword;
        account.MustChange = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetTemporaryPasswordAsync(string userId, string temporaryPassword, CancellationToken cancellationToken)
    {
        _byId[userId].Password = temporaryPassword;
        _byId[userId].MustChange = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisableAsync(string userId, CancellationToken cancellationToken)
    {
        _byId[userId].Disabled = true;
        return Task.FromResult(Result.Success());
    }

    private sealed class Account(string id, EmailAddress email, string displayName, string role)
    {
        public string Id { get; } = id;

        public EmailAddress Email { get; } = email;

        public string Password { get; set; } = string.Empty;

        public bool MustChange { get; set; }

        public bool Disabled { get; set; }

        public int Failures { get; set; }

        public DateTimeOffset LockedUntil { get; set; }

        public UserAccount ToModel() => new(Id, Email, displayName, [role], MustChange, Disabled);
    }
}

internal sealed class FakeTokenIssuer : ITokenIssuer
{
    private int _issued;

    public AccessToken Issue(UserAccount user, DateTimeOffset now) =>
        new($"access-{user.Id}-{++_issued}", now.AddMinutes(60));
}

internal sealed class FakeRefreshTokenCodec : IRefreshTokenCodec
{
    private int _generated;

    public NewRefreshToken Generate()
    {
        var raw = $"rt-{++_generated}";
        return new NewRefreshToken(raw, Hash(raw));
    }

    public string Hash(string rawToken) => $"hash:{rawToken}";
}

internal sealed class FixedTemporaryPasswords : ITemporaryPasswordGenerator
{
    public const string Value = "TempPass-1234-xyz";

    public string Generate() => Value;
}

internal sealed class MinLengthPolicy : IPasswordPolicy
{
    public Result Validate(string password, EmailAddress email) =>
        password.Length >= 12 ? Result.Success() : Error.Validation("password.too_short", "Too short.");
}
