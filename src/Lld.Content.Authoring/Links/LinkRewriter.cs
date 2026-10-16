using System.Text;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Links;

/// <summary>
/// Rewrites link destinations so they work in the app: lessons and cheat sheets become app routes, repository files
/// become GitHub URLs. The app uses a hash router, so a heading is passed as <c>?at=anchor</c> instead of <c>#anchor</c>.
/// Only the destination text changes; everything else in the file stays byte-for-byte the same.
/// </summary>
public sealed class LinkRewriter(LinkResolver resolver, ContentOptions options)
{
    public const string LearnRoute = "/learn";
    public const string ReviseRoute = "/revise";
    public const string AnchorParameter = "at";

    /// <summary>The document body (front-matter removed) with every rewritable link replaced.</summary>
    public string RewriteBody(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var text = new StringBuilder(source.Text);
        foreach (var link in DocumentLinks.Of(source).Where(l => l.Length > 0).OrderByDescending(l => l.Start))
        {
            if (Route(resolver.Resolve(source.Path, link.Url)) is { } route)
            {
                text.Remove(link.Start, link.Length).Insert(link.Start, route);
            }
        }

        return text.ToString(source.BodyStart, text.Length - source.BodyStart);
    }

    /// <summary>The destination to use in the app, or <c>null</c> to keep the link as written.</summary>
    public string? Route(LinkTarget target) => target switch
    {
        LinkTarget.Lesson lesson => $"{LearnRoute}/{Segment(lesson.ModuleSlug)}/{Segment(lesson.LessonSlug)}{At(lesson.Anchor)}",
        LinkTarget.CheatSheet sheet => $"{ReviseRoute}/{Segment(sheet.ModuleSlug)}/{Segment(sheet.LessonSlug)}{At(sheet.Anchor)}",
        LinkTarget.SameDocument same => At(same.Anchor),
        LinkTarget.RepositoryFile file =>
            new Uri(options.RepositoryFileUrl, file.Path).AbsoluteUri + (file.Fragment is null ? string.Empty : $"#{file.Fragment}"),
        _ => null,
    };

    private static string Segment(string value) => Uri.EscapeDataString(value);

    private static string At(string? anchor) =>
        string.IsNullOrEmpty(anchor) ? string.Empty : $"?{AnchorParameter}={Uri.EscapeDataString(anchor)}";
}
