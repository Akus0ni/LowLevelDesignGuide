using System.Collections;

namespace Lld.Samples.Patterns.Behavioral.Iterator.After;

/// <summary>
/// The aggregate: keeps its songs private and hands out an iterator instead. <c>foreach</c> and LINQ work on it, and
/// nobody can change the list from outside.
/// </summary>
public sealed class Playlist : IEnumerable<Song>
{
    private readonly List<Song> _songs = [];

    public void Add(Song song) => _songs.Add(song);

    public IEnumerator<Song> GetEnumerator() => new PlaylistIterator(_songs);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>The iterator, written by hand: remembers where it is and moves one song at a time.</summary>
public sealed class PlaylistIterator(IReadOnlyList<Song> songs) : IEnumerator<Song>
{
    private int _position = -1;

    public Song Current => songs[_position];

    object IEnumerator.Current => Current;

    public bool MoveNext() => ++_position < songs.Count;

    public void Reset() => _position = -1;

    public void Dispose()
    {
        // Nothing to release.
    }
}

public static class Player
{
    public static IReadOnlyList<string> UnplayedTitles(Playlist playlist) =>
        [.. playlist.Where(song => !song.Played).Select(song => song.Title)];
}
