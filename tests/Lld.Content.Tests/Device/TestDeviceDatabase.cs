using Lld.App.Data;
using Lld.App.Data.Content;
using Lld.App.Data.Schema;
using Microsoft.Data.Sqlite;

namespace Lld.Content.Tests.Device;

/// <summary>A device database in a temporary file, deleted afterwards.</summary>
internal sealed class TestDeviceDatabase : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("lld-device-").FullName;

    public TestDeviceDatabase() => Database = new DeviceDatabase(Path.Combine(_folder, DeviceDatabase.FileName));

    public DeviceDatabase Database { get; }

    public Task<int> MigrateAsync(CancellationToken cancellationToken) =>
        new SchemaMigrator(Database, SchemaMigrator.All).MigrateAsync(cancellationToken);

    public async Task<ContentLoadResult> LoadAsync(ContentPack pack, CancellationToken cancellationToken) =>
        await new ContentPackLoader(Database).EnsureLoadedAsync(new FakePackSource(pack), cancellationToken);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }
}

/// <summary>A pack source in memory that counts how often the (big) pack is opened.</summary>
internal sealed class FakePackSource(ContentPack pack, string? version = null) : IContentPackSource
{
    public int PackOpens { get; private set; }

    public Task<string> ReadVersionAsync(CancellationToken cancellationToken) => Task.FromResult(version ?? pack.Version);

    public async Task<Stream> OpenPackAsync(CancellationToken cancellationToken)
    {
        PackOpens++;
        var stream = new MemoryStream();
        await ContentPackJson.WriteAsync(pack, stream, cancellationToken);
        stream.Position = 0;
        return stream;
    }
}
