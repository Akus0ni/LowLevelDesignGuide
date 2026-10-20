using FluentAssertions;
using Lld.App.Data.Notes;
using Lld.App.Data.Progress;
using Lld.Contracts;
using Lld.Domain.Progress;

namespace Lld.Progress.Tests.Device;

public sealed class DeviceNotesTests : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
    private readonly DeviceDatabaseFixture _device = new();
    private readonly SwitchableOwner _owner = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => _device.InitializeAsync();

    public ValueTask DisposeAsync() => _device.DisposeAsync();

    private DeviceNotes Notes => new(new SqliteNotesStore(_device.Database), new DeviceIdentity(_device.Database), _owner);

    [Fact]
    public async Task Save_StampsThisDevicesIdOverWhateverWasSent()
    {
        await Notes.SaveNotesAsync([Note("mine") with { DeviceId = "spoofed" }], Ct);
        await Notes.SaveBookmarksAsync([Lesson() with { DeviceId = "spoofed" }], Ct);

        var deviceId = await new DeviceIdentity(_device.Database).GetIdAsync(Ct);
        var stored = await Notes.GetAsync(Ct);
        stored.Notes.Single().DeviceId.Should().Be(deviceId);
        stored.Bookmarks.Single().DeviceId.Should().Be(deviceId);
    }

    [Fact]
    public async Task EachOwner_SeesOnlyTheirOwnNotes()
    {
        await Notes.SaveNotesAsync([Note("guest's")], Ct);

        _owner.Current = ProgressOwner.User("ada");

        (await Notes.GetAsync(Ct)).Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Adopt_MovesTheGuestsNotesToTheUser()
    {
        await Notes.SaveNotesAsync([Note("guest's")], Ct);
        await Notes.SaveBookmarksAsync([Lesson()], Ct);

        (await Notes.AdoptGuestAsync("ada", Ct)).IsSuccess.Should().BeTrue();
        _owner.Current = ProgressOwner.User("ada");

        (await Notes.GetAsync(Ct)).Should().Match<NotesSnapshotDto>(s => s.Notes.Count == 1 && s.Bookmarks.Count == 1);
    }

    [Fact]
    public async Task Save_RejectsABadId_AnUnknownKind_AndAnEmptyLiveNote()
    {
        (await Notes.SaveNotesAsync([Note("x") with { Id = "not-a-guid" }], Ct)).Error!.Code.Should().Be("validation.notes.id");
        (await Notes.SaveBookmarksAsync([Lesson() with { Kind = "chapter" }], Ct)).Error!.Code.Should().Be("validation.bookmarks.kind");
        (await Notes.SaveNotesAsync([Note(" ")], Ct)).Error!.Code.Should().Be("validation.notes.body");
        (await Notes.GetAsync(Ct)).Notes.Should().BeEmpty();
    }

    private static NoteDto Note(string body) => new(Guid.NewGuid().ToString(), "srp", "", body, T0, T0, null, string.Empty);

    private static BookmarkDto Lesson() => new(BookmarkKinds.Lesson, "srp", "", T0, T0, null, string.Empty);

    private sealed class SwitchableOwner : IProgressOwnerSource
    {
        public ProgressOwner Current { get; set; } = ProgressOwner.Guest;

        public Task<ProgressOwner> GetOwnerAsync(CancellationToken cancellationToken) => Task.FromResult(Current);
    }
}
