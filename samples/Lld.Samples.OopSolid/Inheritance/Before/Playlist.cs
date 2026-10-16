namespace Lld.Samples.OopSolid.Inheritance.Before;

/// <summary>Written without subclasses in mind: everything is virtual, and <c>AddSongs</c> happens to call <c>Add</c>.</summary>
public class Playlist
{
    private readonly List<string> _songs = [];

    public IReadOnlyList<string> Songs => _songs.AsReadOnly();

    public virtual void Add(string song) => _songs.Add(song);

    public virtual void AddSongs(IEnumerable<string> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);
        foreach (var song in songs)
        {
            Add(song);
        }
    }
}

/// <summary>Counts additions by overriding both entry points; it can't see that one calls the other.</summary>
public sealed class CountingPlaylist : Playlist
{
    public int AddedCount { get; private set; }

    public override void Add(string song)
    {
        AddedCount++;
        base.Add(song);
    }

    public override void AddSongs(IEnumerable<string> songs)
    {
        var list = songs.ToList();
        AddedCount += list.Count;
        base.AddSongs(list);
    }
}
