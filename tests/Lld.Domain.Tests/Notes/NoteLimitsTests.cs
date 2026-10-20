using FluentAssertions;
using Lld.Domain.Notes;
using static Lld.Domain.Tests.Notes.NoteBuilder;

namespace Lld.Domain.Tests.Notes;

public sealed class NoteLimitsTests
{
    public static TheoryData<Note, string> InvalidNotes => new()
    {
        { Written() with { Id = Guid.Empty }, "validation.notes.id" },
        { Written(lessonId: ""), "validation.notes.lesson_id" },
        { Written(lessonId: new string('x', NoteLimits.MaxLessonIdLength + 1)), "validation.notes.lesson_id" },
        { Written(anchor: new string('a', NoteLimits.MaxAnchorLength + 1)), "validation.notes.anchor" },
        { Written(new string('b', NoteLimits.MaxBodyLength + 1)), "validation.notes.body" },
        { Written("   "), "validation.notes.body" },
        { Written() with { DeviceId = new string('d', NoteLimits.MaxDeviceIdLength + 1) }, "validation.notes.device_id" },
        { Written() with { UpdatedAt = At(-1) }, "validation.notes.dates" },
    };

    public static TheoryData<Bookmark, string> InvalidBookmarks => new()
    {
        { Marked((BookmarkKind)9), "validation.bookmarks.kind" },
        { Marked(lessonId: " "), "validation.notes.lesson_id" },
        { Marked(BookmarkKind.Section), "validation.bookmarks.anchor" },
        { Marked(BookmarkKind.Lesson, anchor: "the-rule"), "validation.bookmarks.anchor" },
        { Marked(BookmarkKind.CheatSheet, anchor: "the-rule"), "validation.bookmarks.anchor" },
        { Marked(BookmarkKind.Section, anchor: new string('a', NoteLimits.MaxAnchorLength + 1)), "validation.notes.anchor" },
    };

    [Theory]
    [MemberData(nameof(InvalidNotes))]
    public void Validate_RejectsANoteOutsideTheLimits(Note note, string expectedCode) =>
        NoteLimits.Validate(note).Error!.Code.Should().Be(expectedCode);

    [Theory]
    [MemberData(nameof(InvalidBookmarks))]
    public void Validate_RejectsABookmarkOutsideTheLimits(Bookmark bookmark, string expectedCode) =>
        NoteLimits.Validate(bookmark).Error!.Code.Should().Be(expectedCode);

    [Fact]
    public void Validate_AcceptsLessonAndSectionNotes_ADeletedEmptyNote_AndEveryBookmarkKind()
    {
        var emptiedAndClosed = Written("") with { DeletedAt = At(1), UpdatedAt = At(1) };

        NoteLimits.Validate([Written(), Written(anchor: "the-rule"), Written(new string('b', NoteLimits.MaxBodyLength)), emptiedAndClosed])
            .IsSuccess.Should().BeTrue();
        NoteLimits.Validate([Marked(), Marked(BookmarkKind.Section, anchor: "the-rule"), Marked(BookmarkKind.CheatSheet)])
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsTooManyRecordsInOneWrite()
    {
        var notes = Enumerable.Range(0, NoteLimits.MaxRecordsPerWrite + 1).Select(_ => Written()).ToList();
        var bookmarks = Enumerable.Range(0, NoteLimits.MaxRecordsPerWrite + 1).Select(i => Marked(lessonId: $"l{i}")).ToList();

        NoteLimits.Validate(notes).Error!.Code.Should().Be("validation.notes.too_many");
        NoteLimits.Validate(bookmarks).Error!.Code.Should().Be("validation.notes.too_many");
    }

    [Fact]
    public void CheckNoteTotals_RejectsTooManyNotesInAll_OrOnOneLesson()
    {
        NoteLimits.CheckNoteTotals(NoteLimits.MaxNotes, [new("srp", NoteLimits.MaxNotesPerLesson)]).IsSuccess.Should().BeTrue();
        NoteLimits.CheckNoteTotals(NoteLimits.MaxNotes + 1, []).Error!.Code.Should().Be("validation.notes.too_many");
        NoteLimits.CheckNoteTotals(1, [new("srp", NoteLimits.MaxNotesPerLesson + 1)]).Error!.Message.Should().Contain("'srp'");
    }

    [Fact]
    public void CheckBookmarkTotal_RejectsTooManyBookmarks()
    {
        NoteLimits.CheckBookmarkTotal(NoteLimits.MaxBookmarks).IsSuccess.Should().BeTrue();
        NoteLimits.CheckBookmarkTotal(NoteLimits.MaxBookmarks + 1).Error!.Code.Should().Be("validation.bookmarks.too_many");
    }
}
