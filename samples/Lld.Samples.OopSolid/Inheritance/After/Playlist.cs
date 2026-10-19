namespace Lld.Samples.OopSolid.Inheritance.After;

/// <summary>
/// Designed for extension: the public methods aren't virtual, and subclasses get one documented hook that runs exactly
/// once per song, however the song was added. The base class can now change how <c>AddSongs</c> works without
/// breaking anyone.
/// </summary>
public class Playlist
{
    private readonly List<string> _songs = [];

    public IReadOnlyList<string> Songs => _songs.AsReadOnly();

    public void Add(string song)
    {
        _songs.Add(song);
        OnAdded(song);
    }

    public void AddSongs(IEnumerable<string> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);
        foreach (var song in songs)
        {
            Add(song);
        }
    }

    /// <summary>Called once for every song added. Does nothing by default.</summary>
    protected virtual void OnAdded(string song)
    {
    }
}

public sealed class CountingPlaylist : Playlist
{
    public int AddedCount { get; private set; }

    protected override void OnAdded(string song) => AddedCount++;
}
