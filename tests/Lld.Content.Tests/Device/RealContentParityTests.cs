using FluentAssertions;
using Lld.App.Data.Content;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;
using Lld.Content.Tests.Authoring;

namespace Lld.Content.Tests.Device;

/// <summary>For the repository's real content, the device (SQLite) and the dev API (in memory) answer every query identically.</summary>
public sealed class RealContentParityTests : IDisposable
{
    private readonly TestDeviceDatabase _device = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _device.Dispose();

    [Fact]
    public async Task DeviceAndDevApi_ReturnTheSameAnswers()
    {
        var pack = new ContentPipeline(new PhysicalContentFiles(RepositoryContentTests.RepositoryRoot()), new ContentOptions()).Build().Pack!;
        await _device.MigrateAsync(Ct);
        await _device.LoadAsync(pack, Ct);
        var device = new SqliteContentCatalog(_device.Database);
        var devApi = new InMemoryContentCatalog(pack);

        var modules = await devApi.GetModulesAsync(Ct);
        (await device.GetModulesAsync(Ct)).Should().BeEquivalentTo(modules, Strict);
        (await device.GetCheatSheetsAsync(null, Ct)).Should().BeEquivalentTo(await devApi.GetCheatSheetsAsync(null, Ct), Strict);
        foreach (var module in modules)
        {
            var detail = await devApi.GetModuleAsync(module.Slug, Ct);
            (await device.GetModuleAsync(module.Slug, Ct)).Should().BeEquivalentTo(detail, Strict);
            (await device.GetCheatSheetsAsync(module.Slug, Ct)).Should().BeEquivalentTo(await devApi.GetCheatSheetsAsync(module.Slug, Ct), Strict);
            foreach (var lesson in detail!.Lessons)
            {
                (await device.GetLessonAsync(lesson.Id, Ct)).Should().BeEquivalentTo(await devApi.GetLessonAsync(lesson.Id, Ct), Strict);
                (await device.GetCheatSheetAsync(lesson.Id, Ct)).Should().BeEquivalentTo(await devApi.GetCheatSheetAsync(lesson.Id, Ct), Strict);
            }
        }
    }

    private static FluentAssertions.Equivalency.EquivalencyAssertionOptions<T> Strict<T>(
        FluentAssertions.Equivalency.EquivalencyAssertionOptions<T> options) => options.WithStrictOrdering();
}
