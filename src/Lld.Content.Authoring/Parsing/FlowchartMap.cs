using System.Text.RegularExpressions;
using Lld.Contracts;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line inside the map source where the node is first mentioned.</param>
public sealed record MapNode(string Id, int Line);

/// <summary>A module map as written, ready to validate (<see cref="Nodes"/>) and to pack (<see cref="ToContract"/>).</summary>
public sealed record ParsedMap(bool IsFlowchart, IReadOnlyList<MapNode> Nodes, IReadOnlyList<MapGroup> Groups, IReadOnlyList<MapLink> Links)
{
    public ModuleMap ToContract() => new(Groups, Links);
}

/// <summary>
/// Reads a module map (spec 004 §3.6), written as a Mermaid flowchart whose node ids are lesson slugs:
/// <code>
/// flowchart TD
///   subgraph pillars["The four pillars"]
///     encapsulation["Encapsulation"]
///   end
///   ocp -->|"needs"| lsp
/// </code>
/// It understands the subset maps use: <c>subgraph id["Title"]</c> … <c>end</c> (groups), nodes with an optional
/// <c>[label]</c>, and links (<c>--></c>, <c>-.-></c>, <c>==></c>) with an optional <c>|label|</c>, chained and with
/// <c>&amp;</c>. Node labels are ignored: the app shows the lesson's own title. Tokens are separated by spaces.
/// </summary>
public static partial class FlowchartMap
{
    private static readonly string[] Headers = ["flowchart", "graph"];
    private static readonly string[] IgnoredKeywords = ["classDef", "class", "style", "linkStyle", "click", "direction"];
    private const string Comment = "%%";
    private const string SubgraphKeyword = "subgraph";
    private const string EndKeyword = "end";

    public static ParsedMap Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var statements = Statements(source).ToList();
        if (statements.Count == 0 || !IsHeader(statements[0].Text))
        {
            return new ParsedMap(false, [], [], []);
        }

        var builder = new MapBuilder();
        foreach (var (text, line) in statements.Skip(1))
        {
            builder.Add(text, line);
        }

        return builder.Build();
    }

    private static bool IsHeader(string text) =>
        Headers.Any(header => text == header || text.StartsWith(header + ' ', StringComparison.Ordinal));

    private static IEnumerable<(string Text, int Line)> Statements(string source) =>
        source.ReplaceLineEndings("\n").Split('\n')
            .Select((text, index) => (Text: text.Trim().TrimEnd(';'), Line: index + 1))
            .Where(s => s.Text.Length > 0 && !s.Text.StartsWith(Comment, StringComparison.Ordinal));

    private static bool StartsWithKeyword(string text, string keyword) =>
        text == keyword || text.StartsWith(keyword + ' ', StringComparison.Ordinal);

    /// <summary>Collects groups, nodes and links statement by statement.</summary>
    private sealed class MapBuilder
    {
        private const string Ungrouped = "";

        private readonly Dictionary<string, MapNode> _nodes = new(StringComparer.Ordinal);
        private readonly List<(string Title, List<string> Lessons)> _groups = [];
        private readonly List<MapLink> _links = [];
        private (string Title, List<string> Lessons)? _open;

        public void Add(string text, int line)
        {
            if (StartsWithKeyword(text, SubgraphKeyword))
            {
                _open = (SubgraphTitle(text[SubgraphKeyword.Length..].Trim()), []);
                _groups.Add(_open.Value);
            }
            else if (text == EndKeyword)
            {
                _open = null;
            }
            else if (!IgnoredKeywords.Any(keyword => StartsWithKeyword(text, keyword)))
            {
                AddChain(text, line);
            }
        }

        public ParsedMap Build()
        {
            var grouped = _groups.SelectMany(g => g.Lessons).ToHashSet(StringComparer.Ordinal);
            var loose = _nodes.Keys.Where(id => !grouped.Contains(id)).ToList();
            var groups = _groups.Select(g => new MapGroup(g.Title, g.Lessons)).Where(g => g.Lessons.Count > 0).ToList();
            if (loose.Count > 0)
            {
                groups.Add(new MapGroup(Ungrouped, loose));
            }

            return new ParsedMap(true, [.. _nodes.Values], groups, _links);
        }

        /// <summary><c>a &amp; b --&gt;|label| c --&gt; d</c>: every node, and a link between each neighbouring set.</summary>
        private void AddChain(string text, int line)
        {
            List<string> previous = [];
            List<string> current = [];
            (string? Label, bool Pending) arrow = (null, false);
            foreach (Match token in Token().Matches(text))
            {
                if (token.Groups["arrow"].Success)
                {
                    previous = current;
                    current = [];
                    arrow = (Label(token), true);
                }
                else if (token.Groups["node"].Success)
                {
                    var id = token.Groups["node"].Value;
                    AddNode(id, line);
                    current.Add(id);
                    if (arrow.Pending)
                    {
                        _links.AddRange(previous.Select(from => new MapLink(from, id, arrow.Label)));
                    }
                }
            }
        }

        private void AddNode(string id, int line)
        {
            if (_nodes.TryAdd(id, new MapNode(id, line)) && _open is { } group)
            {
                group.Lessons.Add(id);
            }
        }

        private static string? Label(Match token) =>
            token.Groups["label"].Success && token.Groups["label"].Value.Trim().Trim('"') is { Length: > 0 } label ? label : null;

        private static string SubgraphTitle(string rest)
        {
            var match = SubgraphTitlePattern().Match(rest);
            return match.Success ? match.Groups["title"].Value : rest;
        }
    }

    [GeneratedRegex("""(?<arrow><?[-=.]{2,}[>ox]?)(\|(?<label>[^|]*)\|)?|(?<node>[A-Za-z0-9_][\w-]*)(\[[^\]]*\]|\([^)]*\)|\{[^}]*\})?|&""", RegexOptions.CultureInvariant)]
    private static partial Regex Token();

    [GeneratedRegex("""^[\w-]+\s*\[\s*"?(?<title>[^"\]]*)"?\s*\]$""", RegexOptions.CultureInvariant)]
    private static partial Regex SubgraphTitlePattern();
}
