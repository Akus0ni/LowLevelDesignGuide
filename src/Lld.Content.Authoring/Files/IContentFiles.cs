namespace Lld.Content.Authoring.Files;

/// <summary>
/// Read-only view of the repository the content lives in. Paths are relative to the repository root and use
/// forward slashes (<c>content/case-studies/auth/lesson.md</c>), so the same code runs on disk and in memory.
/// </summary>
public interface IContentFiles
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    /// <summary>Names (not paths) of the sub-directories of <paramref name="path"/>, in ordinal order.</summary>
    IReadOnlyList<string> GetDirectories(string path);

    string ReadAllText(string path);
}
