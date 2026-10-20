namespace Lld.Samples.Patterns.Structural.Composite.After;

/// <summary>The component: what a single file and a whole folder both are.</summary>
public interface IFileSystemItem
{
    string Name { get; }

    long Size { get; }
}

/// <summary>A leaf: has no children.</summary>
public sealed class FileItem(string name, long bytes) : IFileSystemItem
{
    public string Name { get; } = name;

    public long Size { get; } = bytes;
}

/// <summary>The composite: holds items (files or folders) and answers by asking each of them.</summary>
public sealed class Folder(string name) : IFileSystemItem
{
    private readonly List<IFileSystemItem> _items = [];

    public string Name { get; } = name;

    public long Size => _items.Sum(item => item.Size);

    public Folder Add(IFileSystemItem item)
    {
        _items.Add(item);
        return this;
    }
}
