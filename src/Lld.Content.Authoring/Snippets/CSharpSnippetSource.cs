using Lld.Content.Authoring.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lld.Content.Authoring.Snippets;

/// <summary>
/// Quotes C# from repository files by symbol (spec 004 §3.3). Each file is parsed once per build. The quoted code keeps
/// its comments, doc comments and attributes, loses blank lines and <c>#region</c>-style directives above it, and is
/// de-indented. Several selectors are joined with one blank line.
/// </summary>
public sealed class CSharpSnippetSource(IContentFiles files) : ICodeSnippetSource
{
    private const string LineFeed = "\n";
    private const string BlankLine = "\n\n";

    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    private readonly Dictionary<string, CompilationUnitSyntax?> _trees = new(StringComparer.Ordinal);
    private readonly HashSet<string> _filesRead = new(StringComparer.Ordinal);

    public IReadOnlySet<string> FilesRead => _filesRead;

    public SnippetResult Extract(SnippetReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (Tree(reference.Path) is not { } root)
        {
            return new SnippetResult.FileMissing(reference.Path);
        }

        if (reference.Selectors.Count == 0)
        {
            return new SnippetResult.Found(Join(TopLevelMembers(root)));
        }

        var nodes = new List<MemberDeclarationSyntax>();
        foreach (var selector in reference.Selectors)
        {
            var found = CSharpSymbolFinder.Find(root, selector);
            switch (found.Count)
            {
                case 0:
                    return new SnippetResult.SymbolMissing(selector);
                case > 1:
                    return new SnippetResult.Ambiguous(selector, found.Count);
                default:
                    nodes.AddRange(found[0].Nodes);
                    break;
            }
        }

        return new SnippetResult.Found(Join(nodes));
    }

    private CompilationUnitSyntax? Tree(string path)
    {
        if (!_trees.TryGetValue(path, out var root))
        {
            root = files.FileExists(path)
                ? CSharpSyntaxTree.ParseText(files.ReadAllText(path), ParseOptions).GetCompilationUnitRoot()
                : null;
            _trees[path] = root;
            if (root is not null)
            {
                _filesRead.Add(path);
            }
        }

        return root;
    }

    /// <summary>The whole file without usings: the members inside a file-scoped namespace, or the top level.</summary>
    private static SyntaxList<MemberDeclarationSyntax> TopLevelMembers(CompilationUnitSyntax root) =>
        root.Members is [FileScopedNamespaceDeclarationSyntax scoped] ? scoped.Members : root.Members;

    private static string Join(IEnumerable<MemberDeclarationSyntax> nodes) =>
        string.Join(BlankLine, nodes.Select(Quote));

    private static string Quote(MemberDeclarationSyntax node)
    {
        // The leading trivia ends with the node's own indentation; lines before it are comments, blanks or directives.
        var leading = node.GetLeadingTrivia().ToFullString().ReplaceLineEndings(LineFeed).Split(LineFeed);
        var comments = leading[..^1].Where(line => line.Trim() is { Length: > 0 } text && !text.StartsWith('#'));
        var declaration = leading[^1] + node.WithoutTrivia().ToFullString().ReplaceLineEndings(LineFeed);
        return Dedent.Apply(string.Join(LineFeed, comments.Append(declaration)));
    }
}
