using FluentAssertions;
using Lld.Domain.Notes;
using static Lld.Domain.Tests.Notes.NoteBuilder;

namespace Lld.Domain.Tests.Notes;

public sealed class NoteRulesTests
{
    [Fact]
    public void Newer_KeepsTheLaterEdit_AndTheIncomingOneOnATie()
    {
        var id = Guid.NewGuid();
        var early = Written("first", minutes: 1, id: id);
        var late = Written("second", minutes: 2, id: id);

        NoteRules.Newer(late, early).Should().BeSameAs(late);
        NoteRules.Newer(early, late).Should().BeSameAs(late);
        NoteRules.Newer(early, early with { Body = "same time" }).Body.Should().Be("same time");
    }

    [Fact]
    public void Newer_LetsALaterUndoBringADeletedNoteBack()
    {
        var note = Written(minutes: 1);
        var deleted = note with { DeletedAt = At(2), UpdatedAt = At(2) };
        var undone = note with { UpdatedAt = At(3) };

        NoteRules.Newer(deleted, undone).IsDeleted.Should().BeFalse();
        NoteRules.Newer(undone, deleted).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Newer_ForBookmarks_LetsALaterRemovalWin()
    {
        var marked = Marked(minutes: 1);
        var removed = marked with { DeletedAt = At(2), UpdatedAt = At(2) };

        NoteRules.Newer(marked, removed).IsDeleted.Should().BeTrue();
        NoteRules.Newer(removed, marked).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Merge_KeepsBothSidesNotes_AndResolvesTheSameIdByTheLaterEdit()
    {
        var shared = Guid.NewGuid();
        var user = new NotesSnapshot([Written("user's", id: shared, minutes: 5), Written("only user")], []);
        var guest = new NotesSnapshot([Written("guest's", id: shared, minutes: 9), Written("only guest")], []);

        var merged = NoteRules.Merge(user, guest);

        merged.Notes.Select(note => note.Body).Should().BeEquivalentTo(["guest's", "only user", "only guest"]);
    }

    [Fact]
    public void Merge_MatchesBookmarksByKind_LessonAndSection()
    {
        var user = new NotesSnapshot([], [Marked(minutes: 5), Marked(BookmarkKind.Section, anchor: "the-rule")]);
        var guest = new NotesSnapshot(
            [],
            [
                Marked(minutes: 9) with { DeletedAt = At(9) },
                Marked(BookmarkKind.Section, anchor: "smell-it"),
                Marked(BookmarkKind.CheatSheet),
            ]);

        var merged = NoteRules.Merge(user, guest);

        merged.Bookmarks.Should().HaveCount(4);
        merged.Bookmarks.Single(bookmark => bookmark.Kind == BookmarkKind.Lesson).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Merge_WithAnEmptySide_GivesTheOtherSide()
    {
        var notes = new NotesSnapshot([Written()], [Marked()]);

        NoteRules.Merge(notes, NotesSnapshot.Empty).Should().BeEquivalentTo(notes);
        NoteRules.Merge(NotesSnapshot.Empty, notes).Should().BeEquivalentTo(notes);
    }
}
