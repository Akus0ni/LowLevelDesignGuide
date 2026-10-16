using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Packing;
using Lld.Content.Authoring.Reading;
using Lld.Content.Authoring.Snippets;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Authoring;

/// <summary>Either a pack or the reasons there isn't one.</summary>
/// <param name="Dependencies">Repository files outside <c>content/</c> the content quotes (snippets), so a change to one
/// can trigger a rebuild.</param>
public sealed record ContentBuildResult(ContentPack? Pack, IReadOnlyList<ContentIssue> Issues, IReadOnlySet<string> Dependencies)
{
    public bool Succeeded => Pack is not null;
}

/// <summary>Facade over read → validate → pack, used by the CLI (build, CI) and the dev API.</summary>
public sealed class ContentPipeline(IContentFiles files, ContentOptions options, ContentValidator validator)
{
    public ContentPipeline(IContentFiles files, ContentOptions options)
        : this(files, options, new ContentValidator())
    {
    }

    public ContentBuildResult Build()
    {
        var set = new ContentReader(files, options).Read();
        var snippets = new CSharpSnippetSource(files);
        IReadOnlyList<ContentIssue> issues =
        [
            .. set.Issues
                .Concat(validator.Validate(new ContentContext(set, files, options, snippets)))
                .OrderBy(i => i.Path, StringComparer.Ordinal)
                .ThenBy(i => i.Line ?? 0)
                .ThenBy(i => i.Code, StringComparer.Ordinal),
        ];
        var pack = issues.Count > 0 ? null : new ContentPacker(options, snippets).Pack(set);
        return new ContentBuildResult(pack, pack is null ? issues : [], snippets.FilesRead);
    }
}
