using FluentAssertions;
using Lld.Domain.Progress;
using Lld.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lld.Progress.Tests.Api;

/// <summary>The API's store on a migrated in-memory SQLite database, with the two contract users created.</summary>
public sealed class EfProgressStoreTests : ProgressStoreContract, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private LldDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(Ct);
        _db = new LldDbContext(new DbContextOptionsBuilder<LldDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync(Ct);
        _db.Users.AddRange(User(Ada), User(Grace));
        await _db.SaveChangesAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    protected override Task<IProgressStore> CreateStoreAsync() => Task.FromResult<IProgressStore>(new EfProgressStore(_db));

    [Fact]
    public async Task Guest_HasNoProgressOnTheServer()
    {
        var save = () => new EfProgressStore(_db).SaveLessonsAsync(ProgressOwner.Guest, [Opened("srp")], Ct);

        await save.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeletingAUser_DeletesTheirProgress()
    {
        var store = new EfProgressStore(_db);
        await store.SaveLessonsAsync(Ada, [Full("srp")], Ct);
        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 19), Ct);

        _db.Users.Remove(await _db.Users.SingleAsync(u => u.Id == Ada.UserId, Ct));
        await _db.SaveChangesAsync(Ct);

        (await _db.LessonProgress.CountAsync(Ct) + await _db.ActivityDays.CountAsync(Ct)).Should().Be(0);
    }

    private static AppUser User(ProgressOwner owner) =>
        new() { Id = owner.UserId!, UserName = owner.UserId, Email = $"{owner.UserId}@example.com" };
}
