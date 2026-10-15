namespace Lld.Content;

/// <summary>File names of the packed content, shared by the build tool (writer) and the app (reader).</summary>
public static class ContentPackFiles
{
    /// <summary>Folder of the app package (MAUI assets) that holds the pack.</summary>
    public const string AssetFolder = "content";

    public const string PackFileName = "content-pack.json";

    /// <summary>Just the version, so the app can skip reading the whole pack when nothing changed.</summary>
    public const string VersionFileName = "content-version.txt";
}
