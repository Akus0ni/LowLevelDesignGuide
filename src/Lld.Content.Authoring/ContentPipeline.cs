using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Packing;
using Lld.Content.Authoring.Reading;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Authoring;

/// <summary>Either a pack or the reasons there isn't one.</summary>
public sealed record ContentBuildResult(ContentPack? Pack, IReadOnlyList<ContentIssue> Issues)
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
        IReadOnlyList<ContentIssue> issues =
        [
            .. set.Issues
                .Concat(validator.Validate(new ContentContext(set, files, options)))
                .OrderBy(i => i.Path, StringComparer.Ordinal)
                .ThenBy(i => i.Line ?? 0)
                .ThenBy(i => i.Code, StringComparer.Ordinal),
        ];
        return issues.Count > 0
            ? new ContentBuildResult(null, issues)
            : new ContentBuildResult(new ContentPacker(options).Pack(set), []);
    }
}
