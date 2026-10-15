using Lld.Content.Authoring.Links;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>Links to lessons, cheat sheets and headings point at something that exists.</summary>
public sealed class InternalLinksRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Documents.SelectMany(document => DocumentLinks.Of(document)
            .Where(link => !link.IsImage)
            .Select(link => Problem(context, document, link))
            .OfType<ContentIssue>());

    private static ContentIssue? Problem(ContentContext context, SourceDocument document, DocumentLink link)
    {
        ContentIssue Issue(string code, string message) => new(document.Path, link.Line, code, message);

        switch (context.Resolver.Resolve(document.Path, link.Url))
        {
            case LinkTarget.Invalid invalid:
                return Issue(IssueCodes.LinkInvalid, $"'{link.Url}': {invalid.Reason}");
            case LinkTarget.SameDocument same when !context.AnchorsOf(document).Contains(same.Anchor):
                return Issue(IssueCodes.LinkAnchorMissing, $"'{link.Url}': this file has no heading '#{same.Anchor}'.");
            case LinkTarget.Lesson lesson:
                return Target(context, lesson.Path(context.Options.ContentDirectory), lesson.Anchor, link, Issue);
            case LinkTarget.CheatSheet sheet:
                return Target(context, sheet.Path(context.Options.ContentDirectory), sheet.Anchor, link, Issue);
            default:
                return null;
        }
    }

    private static ContentIssue? Target(
        ContentContext context, string path, string? anchor, DocumentLink link, Func<string, string, ContentIssue> issue)
    {
        if (context.FindDocument(path) is not { } target)
        {
            return issue(IssueCodes.LinkBroken, $"'{link.Url}': there is no {path}.");
        }

        return anchor is not null && !context.AnchorsOf(target).Contains(anchor)
            ? issue(IssueCodes.LinkAnchorMissing, $"'{link.Url}': {path} has no heading '#{anchor}'.")
            : null;
    }
}

/// <summary>Links to repository files (usually source code) point at a file or folder that exists.</summary>
public sealed class RepositoryLinksRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from link in DocumentLinks.Of(document)
        where !link.IsImage
        let target = context.Resolver.Resolve(document.Path, link.Url) as LinkTarget.RepositoryFile
        where target is not null && !context.Files.FileExists(target.Path) && !context.Files.DirectoryExists(target.Path)
        select new ContentIssue(document.Path, link.Line, IssueCodes.LinkRepositoryFileMissing, $"'{link.Url}': {target.Path} doesn't exist.");
}

/// <summary>Images aren't supported yet (spec 003 §3.1); an image would show as broken in the app.</summary>
public sealed class ImagesNotSupportedRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from link in DocumentLinks.Of(document)
        where link.IsImage
        select new ContentIssue(document.Path, link.Line, IssueCodes.ImageUnsupported, $"'{link.Url}': images aren't supported yet; use a Mermaid diagram.");
}
