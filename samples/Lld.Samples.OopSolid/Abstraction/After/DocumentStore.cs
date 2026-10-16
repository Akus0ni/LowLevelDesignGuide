namespace Lld.Samples.OopSolid.Abstraction.After;

/// <summary>
/// Somewhere to keep named documents. It speaks the caller's language (a name, some text) and promises nothing about
/// folders, streams or paths, so a folder, a blob container or a dictionary can all keep the promise.
/// </summary>
public interface IDocumentStore
{
    Task SaveAsync(string name, string content, CancellationToken cancellationToken);

    /// <summary>The document's content, or <c>null</c> when there is no document with that name.</summary>
    Task<string?> LoadAsync(string name, CancellationToken cancellationToken);
}

/// <summary>Keeps documents as files in one folder; the name never becomes a path outside it.</summary>
public sealed class FolderDocumentStore(string folder) : IDocumentStore
{
    public Task SaveAsync(string name, string content, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(PathOf(name), content, cancellationToken);

    public async Task<string?> LoadAsync(string name, CancellationToken cancellationToken) =>
        File.Exists(PathOf(name)) ? await File.ReadAllTextAsync(PathOf(name), cancellationToken).ConfigureAwait(false) : null;

    private string PathOf(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Path.Combine(folder, Path.GetFileName(name));
    }
}

/// <summary>Keeps documents in memory: for tests and for previews that are never saved.</summary>
public sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal);

    public Task SaveAsync(string name, string content, CancellationToken cancellationToken)
    {
        _documents[name] = content;
        return Task.CompletedTask;
    }

    public Task<string?> LoadAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.GetValueOrDefault(name));
}
