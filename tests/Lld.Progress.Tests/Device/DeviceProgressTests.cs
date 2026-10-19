using FluentAssertions;
using Lld.App.Data.Progress;
using Lld.Contracts;
using Lld.Domain.Progress;

namespace Lld.Progress.Tests.Device;

public sealed class DeviceProgressTests : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
    private readonly DeviceDatabaseFixture _device = new();
    private readonly SwitchableOwner _owner = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => _device.InitializeAsync();

    public ValueTask DisposeAsync() => _device.DisposeAsync();

    private DeviceProgress Progress => new(new SqliteProgressStore(_device.Database), new DeviceIdentity(_device.Database), _owner);

    [Fact]
    public async Task Save_StampsThisDevicesIdOverWhateverWasSent()
    {
        await Progress.SaveLessonsAsync([Opened("srp") with { DeviceId = "spoofed" }], Ct);

        var deviceId = await new DeviceIdentity(_device.Database).GetIdAsync(Ct);
        (await Progress.GetAsync(Ct)).Lessons.Single().DeviceId.Should().Be(deviceId);
    }

    [Fact]
    public async Task EachOwner_SeesOnlyTheirOwnProgress()
    {
        await Progress.SaveLessonsAsync([Opened("srp")], Ct);

        _owner.Current = ProgressOwner.User("ada");

        (await Progress.GetAsync(Ct)).Lessons.Should().BeEmpty();
    }

    [Fact]
    public async Task Adopt_MovesTheGuestsReadingToTheUser()
    {
        await Progress.SaveLessonsAsync([Opened("srp")], Ct);
        await Progress.RecordActivityAsync("2026-10-19", Ct);

        await Progress.AdoptGuestAsync("ada", Ct);
        _owner.Current = ProgressOwner.User("ada");

        (await Progress.GetAsync(Ct)).Should().Match<ProgressSnapshotDto>(s => s.Lessons.Count == 1 && s.ActivityDays.Count == 1);
    }

    [Theory]
    [InlineData("finished", "validation.progress.status")]
    [InlineData("done", "validation.progress.status")]
    public async Task Save_RejectsInvalidRecords(string status, string expectedCode)
    {
        var result = await Progress.SaveLessonsAsync([Opened("srp") with { Status = status }], Ct);

        result.Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task RecordActivity_RejectsABadDay()
    {
        (await Progress.RecordActivityAsync("19/10/2026", Ct)).Error!.Code.Should().Be("validation.progress.day");
    }

    private static LessonProgressDto Opened(string lessonId) =>
        new(lessonId, ProgressStatuses.InProgress, null, null, ["rule"], null, T0, null, T0, string.Empty);

    private sealed class SwitchableOwner : IProgressOwnerSource
    {
        public ProgressOwner Current { get; set; } = ProgressOwner.Guest;

        public Task<ProgressOwner> GetOwnerAsync(CancellationToken cancellationToken) => Task.FromResult(Current);
    }
}
