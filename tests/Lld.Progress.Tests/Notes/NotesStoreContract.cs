using FluentAssertions;
using Lld.Domain.Notes;
using Lld.Domain.Progress;

namespace Lld.Progress.Tests.Notes;

/// <summary>
/// Behaviour every <see cref="INotesStore"/> must have (spec 007 §3.9). The in-memory, device (SQLite) and API (EF Core)
/// stores each derive from this class, so a learner's notes mean the same thing wherever they are kept (Liskov).
/// </summary>
public abstract class NotesStoreContract
{
    protected static readonly DateTimeOffset T0 = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
    protected static readonly ProgressOwner Ada = ProgressOwner.User("user-ada");
    protected static readonly ProgressOwner Grace = ProgressOwner.User("user-grace");

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract Task<INotesStore> CreateStoreAsync();

    [Fact]
    public async Task Get_IsEmptyForAnOwnerWithNothing()
    {
        var store = await CreateStoreAsync();

        (await store.GetAsync(Ada, Ct)).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SaveNotes_KeepsEveryField_OldestFirst()
    {
        var store = await CreateStoreAsync();
        var section = Note("On the rule #weak", anchor: "the-rule", created: 2) with { DeletedAt = At(4), UpdatedAt = At(4) };
        var lesson = Note("Whole lesson", created: 1);

        (await store.SaveNotesAsync(Ada, [section, lesson], Ct)).IsSuccess.Should().BeTrue();

        (await store.GetAsync(Ada, Ct)).Notes.Should().BeEquivalentTo([lesson, section], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task SaveNotes_KeepsTheLaterEdit()
    {
        var store = await CreateStoreAsync();
        var note = Note("first", updated: 5);
        await store.SaveNotesAsync(Ada, [note], Ct);

        await store.SaveNotesAsync(Ada, [note with { Body = "older", UpdatedAt = At(2) }], Ct);
        (await store.GetAsync(Ada, Ct)).Notes.Single().Body.Should().Be("first");

        await store.SaveNotesAsync(Ada, [note with { Body = "newer", UpdatedAt = At(9) }], Ct);
        (await store.GetAsync(Ada, Ct)).Notes.Single().Body.Should().Be("newer");
    }

    [Fact]
    public async Task SaveNotes_ADeleteIsKept_AndALaterUndoBringsTheNoteBack()
    {
        var store = await CreateStoreAsync();
        var note = Note("keep me");
        await store.SaveNotesAsync(Ada, [note with { DeletedAt = At(1), UpdatedAt = At(1) }], Ct);
        (await store.GetAsync(Ada, Ct)).Notes.Single().IsDeleted.Should().BeTrue();

        await store.SaveNotesAsync(Ada, [note with { UpdatedAt = At(2) }], Ct);

        (await store.GetAsync(Ada, Ct)).Notes.Single().IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveBookmarks_KeysByKindLessonAndSection_AndALaterRemovalWins()
    {
        var store = await CreateStoreAsync();
        var lesson = Bookmark(BookmarkKind.Lesson);
        var section = Bookmark(BookmarkKind.Section, anchor: "the-rule");
        var sheet = Bookmark(BookmarkKind.CheatSheet);
        await store.SaveBookmarksAsync(Ada, [lesson, section, sheet, Bookmark(BookmarkKind.Lesson, lessonId: "ocp")], Ct);

        await store.SaveBookmarksAsync(Ada, [lesson with { DeletedAt = At(3), UpdatedAt = At(3) }], Ct);

        var stored = (await store.GetAsync(Ada, Ct)).Bookmarks;
        stored.Should().HaveCount(4);
        stored.Single(b => b.Key == lesson.Key).IsDeleted.Should().BeTrue();
        stored.Single(b => b.Key == section.Key).Should().BeEquivalentTo(section);
    }

    [Fact]
    public async Task Owners_NeverSeeEachOthersNotesOrBookmarks()
    {
        var store = await CreateStoreAsync();
        var adas = Note("Ada's");
        await store.SaveNotesAsync(Ada, [adas], Ct);
        await store.SaveBookmarksAsync(Ada, [Bookmark(BookmarkKind.Lesson)], Ct);

        await store.SaveNotesAsync(Grace, [adas with { Body = "Grace's", UpdatedAt = At(9) }], Ct);

        (await store.GetAsync(Ada, Ct)).Notes.Single().Body.Should().Be("Ada's");
        (await store.GetAsync(Grace, Ct)).Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveNotes_OverALessonsLimit_SavesNothing_AndDeletedNotesDontCount()
    {
        var store = await CreateStoreAsync();
        var full = Enumerable.Range(0, NoteLimits.MaxNotesPerLesson).Select(i => Note($"note {i}")).ToList();
        (await store.SaveNotesAsync(Ada, full, Ct)).IsSuccess.Should().BeTrue();

        var oneMore = await store.SaveNotesAsync(Ada, [Note("one too many"), Note("other lesson", lessonId: "ocp")], Ct);

        oneMore.Error!.Code.Should().Be("validation.notes.too_many");
        (await store.GetAsync(Ada, Ct)).Notes.Should().HaveCount(NoteLimits.MaxNotesPerLesson);

        var deleteOne = full[0] with { DeletedAt = At(5), UpdatedAt = At(5) };
        (await store.SaveNotesAsync(Ada, [deleteOne, Note("now it fits")], Ct)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveNotes_OverTheOwnersLimit_SavesNothing()
    {
        var store = await CreateStoreAsync();
        var lessons = NoteLimits.MaxNotes / NoteLimits.MaxNotesPerLesson;
        foreach (var lesson in Enumerable.Range(0, lessons))
        {
            var notes = Enumerable.Range(0, NoteLimits.MaxNotesPerLesson).Select(i => Note($"n{i}", lessonId: $"l{lesson}")).ToList();
            (await store.SaveNotesAsync(Ada, notes, Ct)).IsSuccess.Should().BeTrue();
        }

        (await store.SaveNotesAsync(Ada, [Note("too many", lessonId: "extra")], Ct)).Error!.Code.Should().Be("validation.notes.too_many");
        (await store.SaveNotesAsync(Grace, [Note("someone else's first")], Ct)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveBookmarks_OverTheLimit_SavesNothing()
    {
        var store = await CreateStoreAsync();
        foreach (var batch in Enumerable.Range(0, NoteLimits.MaxBookmarks).Chunk(NoteLimits.MaxRecordsPerWrite))
        {
            await store.SaveBookmarksAsync(Ada, [.. batch.Select(i => Bookmark(BookmarkKind.Lesson, lessonId: $"l{i}"))], Ct);
        }

        var result = await store.SaveBookmarksAsync(Ada, [Bookmark(BookmarkKind.CheatSheet)], Ct);

        result.Error!.Code.Should().Be("validation.bookmarks.too_many");
        (await store.GetAsync(Ada, Ct)).Bookmarks.Should().HaveCount(NoteLimits.MaxBookmarks);
    }

    [Fact]
    public async Task Merge_AddsTheIncomingRecords_ByTheMergeRule_AndReturnsTheResult()
    {
        var store = await CreateStoreAsync();
        var shared = Note("user's", updated: 2);
        await store.SaveNotesAsync(Ada, [shared, Note("only user's")], Ct);
        await store.SaveBookmarksAsync(Ada, [Bookmark(BookmarkKind.Lesson)], Ct);
        var guest = new NotesSnapshot(
            [shared with { Body = "guest's later edit", UpdatedAt = At(7) }, Note("only guest's")],
            [Bookmark(BookmarkKind.Lesson) with { DeletedAt = At(6), UpdatedAt = At(6) }, Bookmark(BookmarkKind.CheatSheet)]);

        var merged = await store.MergeAsync(Ada, guest, Ct);

        merged.Value.Should().BeEquivalentTo(await store.GetAsync(Ada, Ct));
        merged.Value.Notes.Select(n => n.Body).Should().BeEquivalentTo(["guest's later edit", "only user's", "only guest's"]);
        merged.Value.Bookmarks.Should().HaveCount(2).And.Contain(b => b.Kind == BookmarkKind.Lesson && b.IsDeleted);
    }

    [Fact]
    public async Task Merge_OverALimit_ChangesNothing()
    {
        var store = await CreateStoreAsync();
        var full = Enumerable.Range(0, NoteLimits.MaxNotesPerLesson).Select(i => Note($"note {i}")).ToList();
        await store.SaveNotesAsync(Ada, full, Ct);

        var merged = await store.MergeAsync(Ada, new NotesSnapshot([Note("guest's")], [Bookmark(BookmarkKind.CheatSheet)]), Ct);

        merged.Error!.Code.Should().Be("validation.notes.too_many");
        var stored = await store.GetAsync(Ada, Ct);
        stored.Notes.Should().HaveCount(NoteLimits.MaxNotesPerLesson);
        stored.Bookmarks.Should().BeEmpty();
    }

    protected static Note Note(string body, string lessonId = "srp", string anchor = "", int created = 0, int? updated = null) =>
        new(Guid.NewGuid(), lessonId, anchor, body, At(created), At(updated ?? created), null, "device-a");

    protected static Bookmark Bookmark(BookmarkKind kind, string lessonId = "srp", string anchor = "", int minutes = 0) =>
        new(kind, lessonId, anchor, At(minutes), At(minutes), null, "device-a");

    protected static DateTimeOffset At(int minutes) => T0.AddMinutes(minutes);
}

public sealed class InMemoryNotesStoreTests : NotesStoreContract
{
    protected override Task<INotesStore> CreateStoreAsync() => Task.FromResult<INotesStore>(new InMemoryNotesStore());
}
