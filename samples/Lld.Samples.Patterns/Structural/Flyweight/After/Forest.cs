namespace Lld.Samples.Patterns.Structural.Flyweight.After;

/// <summary>The flyweight: the shared, unchanging part (intrinsic state). One object per kind of tree.</summary>
public sealed class TreeType(string kind, string colour)
{
    public const int TextureBytes = 4096;

    public string Kind { get; } = kind;

    public string Colour { get; } = colour;

    public byte[] Texture { get; } = new byte[TextureBytes];
}

/// <summary>The flyweight factory: hands out the shared object for a kind, creating it only the first time.</summary>
public sealed class TreeTypes
{
    private readonly Dictionary<(string Kind, string Colour), TreeType> _types = [];

    public int Count => _types.Count;

    public TreeType Get(string kind, string colour)
    {
        if (!_types.TryGetValue((kind, colour), out var type))
        {
            type = new TreeType(kind, colour);
            _types[(kind, colour)] = type;
        }

        return type;
    }
}

/// <summary>A tree keeps only what is its own (extrinsic state: where it stands) and points at its shared type.</summary>
public sealed record Tree(int X, int Y, TreeType Type);

public static class Forest
{
    public static IReadOnlyList<Tree> Plant(int count, TreeTypes types) =>
        [.. Enumerable.Range(0, count).Select(i => new Tree(i, i * 2, types.Get(i % 2 == 0 ? "oak" : "pine", "green")))];
}
