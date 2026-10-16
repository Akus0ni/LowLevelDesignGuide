namespace Lld.Content.Authoring.Parsing;

public enum YamlValueKind
{
    /// <summary>A single value: <c>title: Hello</c>.</summary>
    Scalar,

    /// <summary>A list of single values: <c>tags: [a, b]</c>.</summary>
    List,

    /// <summary>Anything else (nested maps, lists of lists); never valid in our front-matter.</summary>
    Other,
}

/// <param name="Line">1-based line of the key in the file.</param>
public sealed record YamlEntry(string Key, int Line, YamlValueKind Kind, string? Scalar, IReadOnlyList<string> Items);

/// <summary>A flat YAML map (front-matter or <c>module.yml</c>) that remembers where each key was written.</summary>
public sealed class YamlMap(IReadOnlyList<YamlEntry> entries, int line)
{
    /// <summary>Entries in the order they were written.</summary>
    public IReadOnlyList<YamlEntry> Entries { get; } = entries;

    /// <summary>1-based line where the map starts, for issues about keys that are missing.</summary>
    public int Line { get; } = line;

    public YamlEntry? this[string key] => Entries.FirstOrDefault(e => e.Key == key);
}
