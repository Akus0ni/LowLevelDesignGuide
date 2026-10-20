using Lld.App.Data;
using Lld.App.Data.Content;
using Lld.App.Data.Notes;
using Lld.App.Data.Schema;
using Lld.Content;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Lld.Search.Sqlite;
using Microsoft.Data.Sqlite;

namespace Lld.Search.Tests.Device;

/// <summary>
/// The device as it really runs: a database file brought to the latest schema, content loaded by
/// <see cref="ContentPackLoader"/>, notes written through <see cref="SqliteNotesStore"/> (the triggers index them).
/// </summary>
internal sealed class DeviceSearchFixture : ISearchFixture
{
    private const string UserPrefix = "user:";

    private readonly string _folder = Directory.CreateTempSubdirectory("lld-search-").FullName;
    private readonly SqliteNotesStore _notes;
    private DateTimeOffset _clock = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    private DeviceSearchFixture()
    {
        Database = new DeviceDatabase(Path.Combine(_folder, DeviceDatabase.FileName));
        Index = new SqliteSearchIndex(Database.OpenAsync);
        _notes = new SqliteNotesStore(Database);
    }

    public DeviceDatabase Database { get; }

    public ISearchIndex Index { get; }

    public SqliteNotesStore Notes => _notes;

    public static async Task<DeviceSearchFixture> CreateAsync(IEnumerable<ISchemaMigration>? migrations = null)
    {
        var fixture = new DeviceSearchFixture();
        await new SchemaMigrator(fixture.Database, migrations ?? SchemaMigrator.All).MigrateAsync(TestContext.Current.CancellationToken);
        return fixture;
    }

    public static ProgressOwner OwnerOf(string ownerKey) =>
        ownerKey.StartsWith(UserPrefix, StringComparison.Ordinal) ? ProgressOwner.User(ownerKey[UserPrefix.Length..]) : ProgressOwner.Guest;

    public async Task LoadContentAsync(ContentPack pack) =>
        await new ContentPackLoader(Database).EnsureLoadedAsync(new PackSource(pack), TestContext.Current.CancellationToken);

    public async Task SaveNoteAsync(string ownerKey, Guid id, string lessonId, string anchor, string body, bool deleted = false)
    {
        _clock = _clock.AddMinutes(1);
        var note = new Note(id, lessonId, anchor, body, _clock, _clock, deleted ? _clock : null, "device-1");
        var saved = await _notes.SaveNotesAsync(OwnerOf(ownerKey), [note], TestContext.Current.CancellationToken);
        if (saved.IsFailure)
        {
            throw new InvalidOperationException(saved.Error!.Message);
        }
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
        return ValueTask.CompletedTask;
    }

    private sealed class PackSource(ContentPack pack) : IContentPackSource
    {
        public Task<string> ReadVersionAsync(CancellationToken cancellationToken) => Task.FromResult(pack.Version);

        public async Task<Stream> OpenPackAsync(CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            await ContentPackJson.WriteAsync(pack, stream, cancellationToken);
            stream.Position = 0;
            return stream;
        }
    }
}
