using FluentAssertions;
using Lld.App.Data;
using Lld.App.Data.Progress;
using Lld.App.Data.Schema;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Lld.Progress.Tests.Device;

/// <summary>A migrated device database in a temporary folder, deleted afterwards.</summary>
public sealed class DeviceDatabaseFixture : IAsyncLifetime
{
    private readonly string _folder = Directory.CreateTempSubdirectory("lld-progress-").FullName;

    public DeviceDatabaseFixture() => Database = new DeviceDatabase(Path.Combine(_folder, DeviceDatabase.FileName));

    public DeviceDatabase Database { get; }

    public async ValueTask InitializeAsync() =>
        await new SchemaMigrator(Database, SchemaMigrator.All).MigrateAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
        return ValueTask.CompletedTask;
    }
}

public sealed class SqliteProgressStoreTests : ProgressStoreContract, IAsyncLifetime
{
    private readonly DeviceDatabaseFixture _device = new();

    public ValueTask InitializeAsync() => _device.InitializeAsync();

    public ValueTask DisposeAsync() => _device.DisposeAsync();

    protected override Task<IProgressStore> CreateStoreAsync() => Task.FromResult<IProgressStore>(Store);

    private SqliteProgressStore Store => new(_device.Database);

    [Fact]
    public async Task AdoptGuest_GivesTheGuestsProgressToTheUserAndEmptiesTheGuest()
    {
        await Store.SaveLessonsAsync(ProgressOwner.Guest, [Full("srp")], Ct);
        await Store.AddActivityDayAsync(ProgressOwner.Guest, new DateOnly(2026, 10, 19), Ct);
        await Store.SaveLessonsAsync(Ada, [Opened("ocp")], Ct);

        var adopted = await Store.AdoptGuestAsync(Ada, Ct);

        adopted.Should().BeTrue();
        (await Store.GetAsync(Ada, Ct)).Should().Match<ProgressSnapshot>(s => s.Lessons.Count == 2 && s.ActivityDays.Count == 1);
        (await Store.GetAsync(ProgressOwner.Guest, Ct)).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task AdoptGuest_DoesNothingWhenTheGuestHasNoProgress()
    {
        await Store.SaveLessonsAsync(Ada, [Opened("ocp")], Ct);

        (await Store.AdoptGuestAsync(Ada, Ct)).Should().BeFalse();
        (await Store.GetAsync(Ada, Ct)).Lessons.Should().ContainSingle();
    }

    [Fact]
    public async Task AdoptGuest_RefusesTheGuestAsTheNewOwner()
    {
        var adopt = () => Store.AdoptGuestAsync(ProgressOwner.Guest, Ct);

        await adopt.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeviceIdentity_IsCreatedOnceAndKept()
    {
        var first = await new DeviceIdentity(_device.Database).GetIdAsync(Ct);

        var later = await new DeviceIdentity(_device.Database).GetIdAsync(Ct);

        later.Should().Be(first).And.HaveLength(32);
    }
}
