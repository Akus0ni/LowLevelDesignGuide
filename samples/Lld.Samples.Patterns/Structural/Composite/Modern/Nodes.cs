using System.Collections.Immutable;

namespace Lld.Samples.Patterns.Structural.Composite.Modern;

/// <summary>
/// Modern C#: when the kinds of node are fixed, a closed set of records and one <c>switch</c> is often simpler, and
/// the compiler warns when a case is missing. Keep the composite when new kinds of node keep arriving.
/// </summary>
public abstract record Node(string Name);

public sealed record FileNode(string Name, long Bytes) : Node(Name);

public sealed record FolderNode(string Name, ImmutableList<Node> Children) : Node(Name);

public static class Nodes
{
    public static long Size(Node node) => node switch
    {
        FileNode file => file.Bytes,
        FolderNode folder => folder.Children.Sum(Size),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
