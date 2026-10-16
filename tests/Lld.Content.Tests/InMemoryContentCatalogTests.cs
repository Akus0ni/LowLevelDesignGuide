namespace Lld.Content.Tests;

public sealed class InMemoryContentCatalogTests : ContentCatalogContract
{
    protected override Task<IContentCatalog> CreateCatalogAsync(ContentPack pack) =>
        Task.FromResult<IContentCatalog>(new InMemoryContentCatalog(pack));
}
