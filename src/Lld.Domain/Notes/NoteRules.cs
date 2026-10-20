namespace Lld.Domain.Notes;

/// <summary>How two versions of a note or bookmark become one. Pure functions, so every store applies them the same way.</summary>
public static class NoteRules
{
    /// <summary>The later edit wins; on a tie the incoming note wins, so saving the same edit twice is harmless.</summary>
    public static Note Newer(Note stored, Note incoming)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.UpdatedAt >= stored.UpdatedAt ? incoming : stored;
    }

    /// <summary>The later edit wins, so a later removal beats an earlier bookmark and the other way round.</summary>
    public static Bookmark Newer(Bookmark stored, Bookmark incoming)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.UpdatedAt >= stored.UpdatedAt ? incoming : stored;
    }

    /// <summary>
    /// Guest adoption (spec 007 §3.8): notes match by id and bookmarks by key, each resolved by <c>Newer</c>.
    /// Ids are GUIDs, so a guest's notes join the user's rather than replace them.
    /// </summary>
    public static NotesSnapshot Merge(NotesSnapshot first, NotesSnapshot second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var notes = first.Notes.ToDictionary(note => note.Id);
        foreach (var note in second.Notes)
        {
            notes[note.Id] = notes.TryGetValue(note.Id, out var existing) ? Newer(existing, note) : note;
        }

        var bookmarks = first.Bookmarks.ToDictionary(bookmark => bookmark.Key);
        foreach (var bookmark in second.Bookmarks)
        {
            bookmarks[bookmark.Key] = bookmarks.TryGetValue(bookmark.Key, out var existing) ? Newer(existing, bookmark) : bookmark;
        }

        return new NotesSnapshot(
            [.. notes.Values.OrderBy(note => note.CreatedAt).ThenBy(note => note.Id)],
            [.. bookmarks.Values.OrderBy(bookmark => bookmark.CreatedAt).ThenBy(bookmark => bookmark.Key.ToString(), StringComparer.Ordinal)]);
    }
}
