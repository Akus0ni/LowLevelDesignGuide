using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Links;

/// <summary>
/// Turns a link as written in a content file into a <see cref="LinkTarget"/>. It only works out where a link points;
/// whether that file or heading exists is checked by the validation rules.
/// </summary>
public sealed partial class LinkResolver(ContentOptions options)
{
    private const char Separator = '/';
    private const int LessonPathSegments = 4; // content/<module>/<lesson>/<file>

    public LinkTarget Resolve(string documentPath, string url)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        if (string.IsNullOrWhiteSpace(url))
        {
            return new LinkTarget.Invalid("The link is empty.");
        }

        if (SchemePattern().IsMatch(url) || url.StartsWith("//", StringComparison.Ordinal))
        {
            return new LinkTarget.External(url);
        }

        var (path, fragment) = SplitFragment(url);
        if (path.Length == 0)
        {
            return fragment is { Length: > 0 }
                ? new LinkTarget.SameDocument(Uri.UnescapeDataString(fragment))
                : new LinkTarget.Invalid("The link is empty.");
        }

        if (path.StartsWith(Separator))
        {
            return new LinkTarget.Invalid("Use a path relative to this file, not one starting with '/'.");
        }

        if (path.Contains('?', StringComparison.Ordinal))
        {
            return new LinkTarget.Invalid("Relative links can't have a query string.");
        }

        var resolved = Combine(Folder(documentPath), Uri.UnescapeDataString(path));
        return resolved is null
            ? new LinkTarget.Invalid("The link points outside the repository.")
            : Classify(resolved, fragment);
    }

    private LinkTarget Classify(string path, string? fragment)
    {
        var segments = path.Split(Separator);
        if (segments[0] != options.ContentDirectory)
        {
            return new LinkTarget.RepositoryFile(path, fragment);
        }

        var anchor = fragment is null ? null : Uri.UnescapeDataString(fragment);
        if (segments.Length == LessonPathSegments)
        {
            switch (segments[3])
            {
                case Reading.ContentReader.LessonFileName:
                    return new LinkTarget.Lesson(segments[1], segments[2], anchor);
                case Reading.ContentReader.CheatSheetFileName:
                    return new LinkTarget.CheatSheet(segments[1], segments[2], anchor);
            }
        }

        return new LinkTarget.Invalid("Inside content/, link to a lesson.md or cheatsheet.md file.");
    }

    private static (string Path, string? Fragment) SplitFragment(string url)
    {
        var hash = url.IndexOf('#', StringComparison.Ordinal);
        return hash < 0 ? (url, null) : (url[..hash], url[(hash + 1)..]);
    }

    private static string Folder(string documentPath)
    {
        var slash = documentPath.LastIndexOf(Separator);
        return slash < 0 ? string.Empty : documentPath[..slash];
    }

    /// <summary>Joins and normalises <c>.</c> / <c>..</c>; <c>null</c> when the result leaves the repository root.</summary>
    private static string? Combine(string folder, string relative)
    {
        var parts = new List<string>(folder.Split(Separator, StringSplitOptions.RemoveEmptyEntries));
        foreach (var segment in relative.Split(Separator))
        {
            switch (segment)
            {
                case "" or ".":
                    continue;
                case "..":
                    if (parts.Count == 0)
                    {
                        return null;
                    }

                    parts.RemoveAt(parts.Count - 1);
                    continue;
                default:
                    parts.Add(segment);
                    continue;
            }
        }

        return parts.Count == 0 ? null : string.Join(Separator, parts);
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.CultureInvariant)]
    private static partial Regex SchemePattern();
}
