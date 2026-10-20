namespace Lld.Samples.Patterns.Behavioral.Iterator.Modern;

/// <summary>
/// Modern C#: <c>yield return</c> makes the compiler write the iterator class. Each method is a different way to walk
/// the same songs, and nothing runs until someone asks for the next one.
/// </summary>
public sealed class Playlist(IEnumerable<Song> songs)
{
    private readonly List<Song> _songs = [.. songs];

    public IEnumerable<Song> Unplayed()
    {
        foreach (var song in _songs)
        {
            if (!song.Played)
            {
                yield return song;
            }
        }
    }

    public IEnumerable<Song> Backwards()
    {
        for (var i = _songs.Count - 1; i >= 0; i--)
        {
            yield return _songs[i];
        }
    }
}
