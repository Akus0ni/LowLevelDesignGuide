using Lld.App.Data.Content;
using Lld.Content;

namespace Lld.App.Content;

/// <summary>Reads the pack that the build put into the app package (MAUI assets under <c>content/</c>).</summary>
public sealed class MauiContentPackSource(IFileSystem fileSystem) : IContentPackSource
{
    public async Task<string> ReadVersionAsync(CancellationToken cancellationToken)
    {
        await using var stream = await fileSystem.OpenAppPackageFileAsync(AssetPath(ContentPackFiles.VersionFileName));
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public Task<Stream> OpenPackAsync(CancellationToken cancellationToken) =>
        fileSystem.OpenAppPackageFileAsync(AssetPath(ContentPackFiles.PackFileName));

    private static string AssetPath(string fileName) => $"{ContentPackFiles.AssetFolder}/{fileName}";
}
