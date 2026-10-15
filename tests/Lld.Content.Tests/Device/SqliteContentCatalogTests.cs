using Lld.App.Data.Content;

namespace Lld.Content.Tests.Device;

/// <summary>The device catalog passes the same contract as the in-memory one the dev API uses.</summary>
public sealed class SqliteContentCatalogTests : ContentCatalogContract, IDisposable
{
    private readonly TestDeviceDatabase _device = new();

    public void Dispose() => _device.Dispose();

    protected override async Task<IContentCatalog> CreateCatalogAsync(ContentPack pack)
    {
        var ct = TestContext.Current.CancellationToken;
        await _device.MigrateAsync(ct);
        await _device.LoadAsync(pack, ct);
        return new SqliteContentCatalog(_device.Database);
    }
}
