using FluentAssertions;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Lld.Infrastructure.Persistence;
using Lld.Progress.Tests.Notes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lld.Progress.Tests.Api;

/// <summary>The API's notes store on a migrated in-memory SQLite database, with the two contract users created.</summary>
public sealed class EfNotesStoreTests : NotesStoreContract, IAsyncLifetime
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

    protected override Task<INotesStore> CreateStoreAsync() => Task.FromResult<INotesStore>(new EfNotesStore(_db));

    [Fact]
    public async Task Guest_HasNoNotesOnTheServer()
    {
        var save = () => new EfNotesStore(_db).SaveNotesAsync(ProgressOwner.Guest, [Note("guest's")], Ct);

        await save.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeletingAUser_DeletesTheirNotesAndBookmarks()
    {
        var store = new EfNotesStore(_db);
        await store.SaveNotesAsync(Ada, [Note("Ada's")], Ct);
        await store.SaveBookmarksAsync(Ada, [Bookmark(BookmarkKind.Lesson)], Ct);

        _db.Users.Remove(await _db.Users.SingleAsync(u => u.Id == Ada.UserId, Ct));
        await _db.SaveChangesAsync(Ct);

        (await _db.Notes.CountAsync(Ct) + await _db.Bookmarks.CountAsync(Ct)).Should().Be(0);
    }

    private static AppUser User(ProgressOwner owner) =>
        new() { Id = owner.UserId!, UserName = owner.UserId, Email = $"{owner.UserId}@example.com" };
}
