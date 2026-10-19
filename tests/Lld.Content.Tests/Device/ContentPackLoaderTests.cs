using FluentAssertions;
using Lld.App.Data.Content;
using Lld.App.Data.Progress;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Lld.Content.Tests.Device;

public sealed class ContentPackLoaderTests : IDisposable
{
    private readonly TestDeviceDatabase _device = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _device.Dispose();

    private ContentPackLoader Loader => new(_device.Database);

    private SqliteContentCatalog Catalog => new(_device.Database);

    [Fact]
    public async Task FirstLaunch_LoadsThePack()
    {
        await _device.MigrateAsync(Ct);

        var result = await Loader.EnsureLoadedAsync(new FakePackSource(TestPacks.Sample()), Ct);

        result.Should().Be(new ContentLoadResult(Loaded: true, "v-sample"));
        (await Catalog.GetModulesAsync(Ct)).Should().HaveCount(3);
        await using var connection = await _device.Database.OpenAsync(Ct);
        (await ContentPackLoader.ReadLoadedVersionAsync(connection, Ct)).Should().Be("v-sample");
    }

    [Fact]
    public async Task SameVersion_DoesNotEvenOpenThePack()
    {
        await _device.MigrateAsync(Ct);
        await _device.LoadAsync(TestPacks.Sample(), Ct);
        var source = new FakePackSource(TestPacks.Sample());

        var result = await Loader.EnsureLoadedAsync(source, Ct);

        result.Loaded.Should().BeFalse();
        source.PackOpens.Should().Be(0);
    }

    [Fact]
    public async Task NewVersion_ReplacesAllContent()
    {
        await _device.MigrateAsync(Ct);
        await _device.LoadAsync(TestPacks.Sample(), Ct);
        var smaller = TestPacks.Sample() with { Version = "v-2", Modules = [TestPacks.Sample().Modules[1]] };

        var result = await Loader.EnsureLoadedAsync(new FakePackSource(smaller), Ct);

        result.Should().Be(new ContentLoadResult(Loaded: true, "v-2"));
        (await Catalog.GetModulesAsync(Ct)).Select(m => m.Slug).Should().Equal("start");
        (await Catalog.GetLessonAsync("case-study-auth", Ct)).Should().BeNull();
    }

    [Fact]
    public async Task NewVersion_KeepsEveryonesProgress_EvenForLessonsThatLeft()
    {
        await _device.MigrateAsync(Ct);
        await _device.LoadAsync(TestPacks.Sample(), Ct);
        var progress = new SqliteProgressStore(_device.Database);
        var opened = new LessonProgress("case-study-auth", LessonStatus.InProgress, null, null, ["rule"], null,
            DateTimeOffset.UnixEpoch, null, DateTimeOffset.UnixEpoch, "device-a");
        await progress.SaveLessonsAsync(ProgressOwner.Guest, [opened], Ct);
        var smaller = TestPacks.Sample() with { Version = "v-2", Modules = [TestPacks.Sample().Modules[1]] };

        await Loader.EnsureLoadedAsync(new FakePackSource(smaller), Ct);

        (await progress.GetAsync(ProgressOwner.Guest, Ct)).Lessons.Should().ContainSingle(l => l.LessonId == "case-study-auth");
    }

    [Fact]
    public async Task FailureMidLoad_KeepsThePreviousContentAndVersion()
    {
        await _device.MigrateAsync(Ct);
        await _device.LoadAsync(TestPacks.Sample(), Ct);
        var sample = TestPacks.Sample();
        var start = sample.Modules[1];
        var clash = start with { Slug = "start-again", Order = 9 }; // Same lesson id twice: the insert fails halfway.
        var broken = sample with { Version = "v-broken", Modules = [.. sample.Modules, clash] };

        var load = () => Loader.EnsureLoadedAsync(new FakePackSource(broken), Ct);

        await load.Should().ThrowAsync<SqliteException>();
        (await Catalog.GetModulesAsync(Ct)).Select(m => m.Slug).Should().Equal("start", "oop-solid", "case-studies");
        await using var connection = await _device.Database.OpenAsync(Ct);
        (await ContentPackLoader.ReadLoadedVersionAsync(connection, Ct)).Should().Be("v-sample");
    }

    [Fact]
    public async Task PackThatDisagreesWithItsVersionFile_IsRejected()
    {
        await _device.MigrateAsync(Ct);

        var load = () => Loader.EnsureLoadedAsync(new FakePackSource(TestPacks.Sample(), version: "v-other"), Ct);

        await load.Should().ThrowAsync<InvalidDataException>().WithMessage("*v-sample*v-other*");
    }

    [Fact]
    public async Task VersionFileWhitespace_IsIgnored()
    {
        await _device.MigrateAsync(Ct);

        var result = await Loader.EnsureLoadedAsync(new FakePackSource(TestPacks.Sample(), version: "v-sample\n"), Ct);

        result.Should().Be(new ContentLoadResult(Loaded: true, "v-sample"));
    }
}
