namespace Lld.Samples.Patterns.Structural.Composite.Before;

public sealed class FileItem(string name, long bytes)
{
    public string Name { get; } = name;

    public long Bytes { get; } = bytes;
}

public sealed class Folder(string name)
{
    public string Name { get; } = name;

    public List<object> Items { get; } = [];
}

/// <summary>
/// Every caller that walks the tree checks types itself. Size, count, search: each repeats this <c>if</c> chain, and a
/// new kind of item (a shortcut) means finding them all.
/// </summary>
public static class Sizes
{
    public static long SizeOf(object item) => item switch
    {
        FileItem file => file.Bytes,
        Folder folder => folder.Items.Sum(SizeOf),
        _ => throw new ArgumentException($"Unknown item {item}.", nameof(item)),
    };
}
