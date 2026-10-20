namespace Lld.Samples.Patterns.Structural.Flyweight.Before;

/// <summary>
/// Every tree carries its own copy of the heavy part (name, colour and a 4 KB texture), although thousands of trees
/// look exactly alike. A forest of 10,000 trees holds 10,000 textures.
/// </summary>
public sealed class Tree(int x, int y, string kind, string colour)
{
    public const int TextureBytes = 4096;

    public int X { get; } = x;

    public int Y { get; } = y;

    public string Kind { get; } = kind;

    public string Colour { get; } = colour;

    public byte[] Texture { get; } = new byte[TextureBytes];
}

public static class Forest
{
    public static IReadOnlyList<Tree> Plant(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new Tree(i, i * 2, i % 2 == 0 ? "oak" : "pine", "green"))];
}
