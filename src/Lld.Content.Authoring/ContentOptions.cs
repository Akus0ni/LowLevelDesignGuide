namespace Lld.Content.Authoring;

/// <summary>Settings for reading, validating and packing <c>content/</c>.</summary>
public sealed record ContentOptions
{
    public static readonly Uri DefaultRepositoryFileUrl = new("https://github.com/Akus0ni/LowLevelDesignGuide/blob/main/");

    public const int DefaultWordsPerMinute = 200;

    /// <summary>The content folder, relative to the repository root.</summary>
    public string ContentDirectory { get; init; } = "content";

    /// <summary>Links to repository files (e.g. <c>../../../src/X.cs</c>) are rewritten to this base, so they open on GitHub.</summary>
    public Uri RepositoryFileUrl { get; init; } = DefaultRepositoryFileUrl;

    public int WordsPerMinute { get; init; } = DefaultWordsPerMinute;
}
