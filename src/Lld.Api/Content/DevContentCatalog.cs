using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lld.Content;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;

namespace Lld.Api.Content;

/// <summary>Either a catalog to serve or the reasons the content can't be packed.</summary>
internal sealed record DevContentState(IContentCatalog? Catalog, IReadOnlyList<ContentIssue> Issues);

/// <summary>
/// Browser dev mode's content (spec 003): <c>content/</c> read straight from the repository with the same pipeline the
/// app build uses. Each request compares a fingerprint of the folder (paths, sizes, write times) with the last one, so
/// a saved, added or deleted file shows up on the next page load, without a watcher that could miss events.
/// </summary>
internal sealed class DevContentCatalog(string repositoryRoot, ContentOptions options) : IDisposable
{
    private readonly SemaphoreSlim _rebuild = new(1, 1);
    private readonly string _contentFolder = Path.Combine(repositoryRoot, options.ContentDirectory);
    private DevContentState? _state;
    private string? _fingerprint;

    public string ContentFolder => _contentFolder;

    public async Task<DevContentState> GetAsync(CancellationToken cancellationToken)
    {
        var fingerprint = Fingerprint();
        await _rebuild.WaitAsync(cancellationToken);
        try
        {
            if (_state is null || fingerprint != _fingerprint)
            {
                var result = new ContentPipeline(new PhysicalContentFiles(repositoryRoot), options).Build();
                _state = new DevContentState(result.Pack is { } pack ? new InMemoryContentCatalog(pack) : null, result.Issues);
                _fingerprint = fingerprint;
            }

            return _state;
        }
        finally
        {
            _rebuild.Release();
        }
    }

    public void Dispose() => _rebuild.Dispose();

    private string Fingerprint()
    {
        if (!Directory.Exists(_contentFolder))
        {
            return string.Empty;
        }

        var listing = new StringBuilder();
        foreach (var file in new DirectoryInfo(_contentFolder)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .OrderBy(f => f.FullName, StringComparer.Ordinal))
        {
            listing.Append(file.FullName).Append('|')
                .Append(file.Length.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(listing.ToString())));
    }
}
