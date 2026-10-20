using FluentAssertions;
using Lld.App.Data.Notes;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Lld.Progress.Tests.Notes;

namespace Lld.Progress.Tests.Device;

public sealed class SqliteNotesStoreTests : NotesStoreContract, IAsyncLifetime
{
    private readonly DeviceDatabaseFixture _device = new();

    public ValueTask InitializeAsync() => _device.InitializeAsync();

    public ValueTask DisposeAsync() => _device.DisposeAsync();

    protected override Task<INotesStore> CreateStoreAsync() => Task.FromResult<INotesStore>(Store);

    private SqliteNotesStore Store => new(_device.Database);

    [Fact]
    public async Task AdoptGuest_GivesTheGuestsNotesToTheUserAndEmptiesTheGuest()
    {
        await Store.SaveNotesAsync(ProgressOwner.Guest, [Note("guest's")], Ct);
        await Store.SaveBookmarksAsync(ProgressOwner.Guest, [Bookmark(BookmarkKind.CheatSheet)], Ct);
        await Store.SaveNotesAsync(Ada, [Note("Ada's")], Ct);

        var adopted = await Store.AdoptGuestAsync(Ada, Ct);

        adopted.Value.Should().BeTrue();
        (await Store.GetAsync(Ada, Ct)).Should().Match<NotesSnapshot>(s => s.Notes.Count == 2 && s.Bookmarks.Count == 1);
        (await Store.GetAsync(ProgressOwner.Guest, Ct)).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task AdoptGuest_DoesNothingWhenTheGuestHasNothing()
    {
        (await Store.AdoptGuestAsync(Ada, Ct)).Value.Should().BeFalse();
    }

    [Fact]
    public async Task AdoptGuest_OverALimit_LeavesBothAsTheyWere()
    {
        await Store.SaveNotesAsync(Ada, [.. Enumerable.Range(0, NoteLimits.MaxNotesPerLesson).Select(i => Note($"n{i}"))], Ct);
        await Store.SaveNotesAsync(ProgressOwner.Guest, [Note("one more")], Ct);

        var adopted = await Store.AdoptGuestAsync(Ada, Ct);

        adopted.Error!.Code.Should().Be("validation.notes.too_many");
        (await Store.GetAsync(ProgressOwner.Guest, Ct)).Notes.Should().ContainSingle();
        (await Store.GetAsync(Ada, Ct)).Notes.Should().HaveCount(NoteLimits.MaxNotesPerLesson);
    }

    [Fact]
    public async Task AdoptGuest_RefusesTheGuestAsTheNewOwner()
    {
        var adopt = () => Store.AdoptGuestAsync(ProgressOwner.Guest, Ct);

        await adopt.Should().ThrowAsync<ArgumentException>();
    }
}
