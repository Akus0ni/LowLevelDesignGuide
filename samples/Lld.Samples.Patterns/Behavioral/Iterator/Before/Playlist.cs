namespace Lld.Samples.Patterns.Behavioral.Iterator.Before;

/// <summary>
/// The playlist hands out its inner list. Callers loop with indexes, can clear or reorder it, and the playlist can
/// never switch to another storage (a linked list, a database page) without breaking them.
/// </summary>
public sealed class Playlist
{
    public List<Song> Songs { get; } = [];
}

public static class Player
{
    public static IReadOnlyList<string> UnplayedTitles(Playlist playlist)
    {
        var titles = new List<string>();
        for (var i = 0; i < playlist.Songs.Count; i++)
        {
            if (!playlist.Songs[i].Played)
            {
                titles.Add(playlist.Songs[i].Title);
            }
        }

        return titles;
    }
}
