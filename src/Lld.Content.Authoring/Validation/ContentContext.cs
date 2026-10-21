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
    private readonly Lazy<SearchableContent> _searchable;

    public ContentContext(ContentSet set, IContentFiles files, ContentOptions options, ICodeSnippetSource snippets)
    {
        ArgumentNullException.ThrowIfNull(set);
        Set = set;
        Files = files;
        Options = options;
        Snippets = snippets;
        Resolver = new LinkResolver(options);
        Lessons = [.. set.Modules.SelectMany(m => m.Lessons)];
        PatternModules = [.. set.Modules.Where(m => TemplateOf(m) == ContentSchemas.PatternTemplate)];
        PatternLessons = [.. PatternModules.SelectMany(m => m.Lessons)];
        DiagramLessons = [.. set.Modules.Where(m => TemplateOf(m) == ContentSchemas.DiagramTemplate).SelectMany(m => m.Lessons)];
        ConceptLessons = [.. set.Modules
            .Where(m => TemplateOf(m) is ContentSchemas.ConceptTemplate or ContentSchemas.PatternTemplate or ContentSchemas.DiagramTemplate)
            .SelectMany(m => m.Lessons)];
        Documents = [.. Lessons.SelectMany(l => new[] { l.Lesson, l.CheatSheet }).OfType<SourceDocument>()];
        _documentsByPath = Documents.ToDictionary(d => d.Path, StringComparer.Ordinal);
        _unreadable = [.. set.Issues.Select(i => i.Path)];
        _searchable = new(() => new SearchableContent(Documents));
    }

    public ContentSet Set { get; }

    public IContentFiles Files { get; }

    public ContentOptions Options { get; }

    public LinkResolver Resolver { get; }

    /// <summary>Shared with the packer, so each quoted file is parsed once per build.</summary>
    public ICodeSnippetSource Snippets { get; }

    public IReadOnlyList<SourceLesson> Lessons { get; }

    /// <summary>
    /// Lessons of modules with <c>template: concept</c> (spec 004 §3.2), <c>template: pattern</c> (spec 006 §3.2) or
    /// <c>template: diagram</c> (spec 008 §3.2), which keeps every concept rule.
    /// </summary>
    public IReadOnlyList<SourceLesson> ConceptLessons { get; }

    /// <summary>Modules with <c>template: pattern</c> (spec 006).</summary>
    public IReadOnlyList<SourceModule> PatternModules { get; }

    /// <summary>Lessons of <see cref="PatternModules"/>; they are also in <see cref="ConceptLessons"/>.</summary>
    public IReadOnlyList<SourceLesson> PatternLessons { get; }

    /// <summary>Lessons of modules with <c>template: diagram</c> (spec 008); they are also in <see cref="ConceptLessons"/>.</summary>
    public IReadOnlyList<SourceLesson> DiagramLessons { get; }

    /// <summary>Every lesson and cheat sheet that exists.</summary>
    public IReadOnlyList<SourceDocument> Documents { get; }

    /// <summary>The search text of every section, as words, computed once (spec 009 vocabulary rules).</summary>
    public SearchableContent Searchable => _searchable.Value;

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

    private static string? TemplateOf(SourceModule module) => module.Manifest?[FrontMatterKeys.Template]?.Scalar;
}
