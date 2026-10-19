using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

public enum YamlValueKind
{
    /// <summary>A single value: <c>title: Hello</c>.</summary>
    Scalar,

    /// <summary>A list of single values: <c>tags: [a, b]</c>.</summary>
    List,

    /// <summary>
    /// Anything else (nested maps, lists of maps). Only keys declared <c>Structured</c> accept it; their own parser
    /// reads <see cref="YamlEntry.Nested"/>.
    /// </summary>
    Other,
}

/// <param name="Line">1-based line of the key in the file.</param>
/// <param name="Nested">The value as a YAML tree, for <see cref="YamlValueKind.Other"/> values.</param>
public sealed record YamlEntry(string Key, int Line, YamlValueKind Kind, string? Scalar, IReadOnlyList<string> Items, YamlNested? Nested = null);

/// <summary>A nested YAML value that still knows where each of its nodes sits in the file.</summary>
/// <param name="LineOffset">Add a node's 1-based line in the parsed text to get its line in the file.</param>
public sealed record YamlNested(YamlNode Root, int LineOffset)
{
    public int LineOf(YamlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return LineOffset + (int)node.Start.Line;
    }
}

/// <summary>A flat YAML map (front-matter or <c>module.yml</c>) that remembers where each key was written.</summary>
public sealed class YamlMap(IReadOnlyList<YamlEntry> entries, int line)
{
    /// <summary>Entries in the order they were written.</summary>
    public IReadOnlyList<YamlEntry> Entries { get; } = entries;

    /// <summary>1-based line where the map starts, for issues about keys that are missing.</summary>
    public int Line { get; } = line;

    public YamlEntry? this[string key] => Entries.FirstOrDefault(e => e.Key == key);
}
