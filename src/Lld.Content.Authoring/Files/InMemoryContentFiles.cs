namespace Lld.Content.Authoring.Files;

/// <summary><see cref="IContentFiles"/> over a dictionary of path → text; directories are implied by file paths. For tests.</summary>
public sealed class InMemoryContentFiles : IContentFiles
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public InMemoryContentFiles Add(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(path);
        _files[path] = text;
        return this;
    }

    public bool FileExists(string path) => _files.ContainsKey(path);

    public bool DirectoryExists(string path) => _files.Keys.Any(file => file.StartsWith(Prefix(path), StringComparison.Ordinal));

    public IReadOnlyList<string> GetDirectories(string path)
    {
        var prefix = Prefix(path);
        return [.. _files.Keys
            .Where(file => file.StartsWith(prefix, StringComparison.Ordinal))
            .Select(file => file[prefix.Length..])
            .Where(rest => rest.Contains('/', StringComparison.Ordinal))
            .Select(rest => rest[..rest.IndexOf('/', StringComparison.Ordinal)])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
    }

    public string ReadAllText(string path) =>
        _files.TryGetValue(path, out var text) ? text : throw new FileNotFoundException($"No file '{path}'.", path);

    private static string Prefix(string path) => path.Length == 0 ? string.Empty : path.TrimEnd('/') + "/";
}
