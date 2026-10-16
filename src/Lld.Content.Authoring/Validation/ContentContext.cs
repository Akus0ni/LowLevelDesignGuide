using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Links;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;
using Lld.Content.Authoring.Snippets;

namespace Lld.Content.Authoring.Validation;

/// <summary>What the rules look at: the content that was read, the repository files and shared lookups.</summary>
public sealed class ContentContext
{
    private readonly Dictionary<string, SourceDocument> _documentsByPath;
    private readonly Dictionary<string, IReadOnlySet<string>> _anchors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _unreadable;

    public ContentContext(ContentSet set, IContentFiles files, ContentOptions options, ICodeSnippetSource snippets)
    {
        ArgumentNullException.ThrowIfNull(set);
        Set = set;
        Files = files;
        Options = options;
        Snippets = snippets;
        Resolver = new LinkResolver(options);
        Lessons = [.. set.Modules.SelectMany(m => m.Lessons)];
        ConceptLessons = [.. set.Modules
            .Where(m => m.Manifest?[FrontMatterKeys.Template]?.Scalar == ContentSchemas.ConceptTemplate)
            .SelectMany(m => m.Lessons)];
        Documents = [.. Lessons.SelectMany(l => new[] { l.Lesson, l.CheatSheet }).OfType<SourceDocument>()];
        _documentsByPath = Documents.ToDictionary(d => d.Path, StringComparer.Ordinal);
        _unreadable = [.. set.Issues.Select(i => i.Path)];
    }

    public ContentSet Set { get; }

    public IContentFiles Files { get; }

    public ContentOptions Options { get; }

    public LinkResolver Resolver { get; }

    /// <summary>Shared with the packer, so each quoted file is parsed once per build.</summary>
    public ICodeSnippetSource Snippets { get; }

    public IReadOnlyList<SourceLesson> Lessons { get; }

    /// <summary>Lessons of modules with <c>template: concept</c> (spec 004 §3.2).</summary>
    public IReadOnlyList<SourceLesson> ConceptLessons { get; }

    /// <summary>Every lesson and cheat sheet that exists.</summary>
    public IReadOnlyList<SourceDocument> Documents { get; }

    public SourceDocument? FindDocument(string path) => _documentsByPath.GetValueOrDefault(path);

    /// <summary>True when the reader already reported the file (e.g. unreadable YAML), so rules don't pile on.</summary>
    public bool WasUnreadable(string path) => _unreadable.Contains(path);

    /// <summary>The heading anchors of a document, computed once.</summary>
    public IReadOnlySet<string> AnchorsOf(SourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_anchors.TryGetValue(document.Path, out var anchors))
        {
            anchors = DocumentHeadings.Of(document).Select(h => h.Anchor).ToHashSet(StringComparer.Ordinal);
            _anchors[document.Path] = anchors;
        }

        return anchors;
    }
}
