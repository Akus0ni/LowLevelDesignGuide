namespace Lld.Content.Authoring.Files;

/// <summary><see cref="IContentFiles"/> over a repository folder on disk.</summary>
public sealed class PhysicalContentFiles : IContentFiles
{
    private readonly string _root;

    public PhysicalContentFiles(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        _root = Path.GetFullPath(repositoryRoot);
    }

    public bool FileExists(string path) => File.Exists(Resolve(path));

    public bool DirectoryExists(string path) => Directory.Exists(Resolve(path));

    public IReadOnlyList<string> GetDirectories(string path)
    {
        var full = Resolve(path);
        if (!Directory.Exists(full))
        {
            return [];
        }

        return [.. Directory.EnumerateDirectories(full)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];
    }

    public string ReadAllText(string path) => File.ReadAllText(Resolve(path));

    private string Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var full = Path.GetFullPath(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar)));
        var inside = full.Equals(_root, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        return inside ? full : throw new ArgumentException($"'{path}' is outside the repository.", nameof(path));
    }
}
