using FluentAssertions;
using Lld.Contracts;
using Lld.Domain.Notes;
using Lld.Wire;

namespace Lld.Progress.Tests;

public sealed class NotesWireTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Snapshot_SurvivesTheRoundTrip()
    {
        var snapshot = new NotesSnapshot(
            [
                new Note(Guid.NewGuid(), "srp", "the-rule", "Ask #weak", T0, T0.AddMinutes(1), null, "d"),
                new Note(Guid.NewGuid(), "srp", "", "", T0, T0, T0, "d"),
            ],
            [
                new Bookmark(BookmarkKind.Lesson, "srp", "", T0, T0, null, "d"),
                new Bookmark(BookmarkKind.Section, "srp", "the-rule", T0, T0, T0, "d"),
                new Bookmark(BookmarkKind.CheatSheet, "ocp", "", T0, T0, null, "d"),
            ]);

        var back = NotesWire.ToSnapshot(NotesWire.ToDto(snapshot));

        back.Value.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public void Dto_UsesTheWireNames()
    {
        var id = Guid.NewGuid();

        NotesWire.ToDto(new Note(id, "srp", "", "x", T0, T0, null, "d")).Id.Should().Be(id.ToString("D"));
        NotesWire.ToDto(new Bookmark(BookmarkKind.CheatSheet, "srp", "", T0, T0, null, "d")).Kind.Should().Be("cheatSheet");
    }

    [Fact]
    public void ABadIdOrKind_IsAValidationError()
    {
        NotesWire.ToNote(new NoteDto("42", "srp", "", "x", T0, T0, null, "d")).Error!.Code.Should().Be("validation.notes.id");
        NotesWire.ToBookmark(new BookmarkDto("chapter", "srp", "", T0, T0, null, "d")).Error!.Code.Should().Be("validation.bookmarks.kind");
    }
}
