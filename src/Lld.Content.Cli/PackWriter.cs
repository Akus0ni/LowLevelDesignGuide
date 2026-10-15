using System.Text;

namespace Lld.Content.Cli;

/// <summary>
/// Writes <c>content-pack.json</c> and <c>content-version.txt</c>. When the folder already holds the same version
/// nothing is touched, so file timestamps (and the app's incremental asset copy) stay as they are.
/// </summary>
internal static class PackWriter
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <returns><c>true</c> when files were written; <c>false</c> when they were already up to date.</returns>
    public static async Task<bool> WriteAsync(ContentPack pack, DirectoryInfo folder, CancellationToken cancellationToken)
    {
        folder.Create();
        var packPath = Path.Combine(folder.FullName, ContentPackFiles.PackFileName);
        var versionPath = Path.Combine(folder.FullName, ContentPackFiles.VersionFileName);
        if (File.Exists(packPath) && File.Exists(versionPath)
            && await File.ReadAllTextAsync(versionPath, cancellationToken) == pack.Version)
        {
            return false;
        }

        await using (var stream = File.Create(packPath))
        {
            await ContentPackJson.WriteAsync(pack, stream, cancellationToken);
        }

        // Written last: a pack without a matching version file is never mistaken for an up-to-date one.
        await File.WriteAllTextAsync(versionPath, pack.Version, Utf8, cancellationToken);
        return true;
    }
}
