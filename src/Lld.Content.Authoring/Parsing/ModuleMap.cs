using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line inside the map source.</param>
public sealed record MapNode(string Id, int Line);

/// <summary>
/// Reads a module map (spec 004 §3.6): a Mermaid flowchart whose node ids are lesson slugs. A small scanner, not a
/// Mermaid parser: labels (<c>[...]</c>, <c>(...)</c>, <c>{...}</c>, <c>"..."</c>, <c>|...|</c>) are removed, and what is
/// left between arrows is a node id. Arrows must have a space on each side (<c>a --> b</c>).
/// </summary>
public static partial class ModuleMap
{
    private static readonly string[] Headers = ["flowchart", "graph"];
    private static readonly string[] StatementKeywords = ["subgraph", "end", "classDef", "class", "style", "linkStyle", "click", "direction"];
    private const string Comment = "%%";
    private const string ClassShorthand = ":::";
    private const string NodeGroup = "&";

    public static bool IsFlowchart(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var first = Statements(source).FirstOrDefault().Text ?? string.Empty;
        return Headers.Any(header => first == header || first.StartsWith(header + ' ', StringComparison.Ordinal));
    }

    /// <summary>Every node id mentioned, with the line it is first mentioned on.</summary>
    public static IReadOnlyList<MapNode> Nodes(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var nodes = new Dictionary<string, MapNode>(StringComparer.Ordinal);
        foreach (var (text, line) in Statements(source).Skip(1).Where(s => !IsKeywordStatement(s.Text)))
        {
            foreach (var token in Labels().Replace(text, " ").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var id = token.Split(ClassShorthand)[0];
                if (id.Length > 0 && id != NodeGroup && !Arrow().IsMatch(id))
                {
                    nodes.TryAdd(id, new MapNode(id, line));
                }
            }
        }

        return [.. nodes.Values];
    }

    private static IEnumerable<(string Text, int Line)> Statements(string source) =>
        source.ReplaceLineEndings("\n").Split('\n')
            .Select((text, index) => (Text: text.Trim().TrimEnd(';'), Line: index + 1))
            .Where(s => s.Text.Length > 0 && !s.Text.StartsWith(Comment, StringComparison.Ordinal));

    private static bool IsKeywordStatement(string text) =>
        StatementKeywords.Any(keyword => text == keyword || text.StartsWith(keyword + ' ', StringComparison.Ordinal));

    [GeneratedRegex("""\[[^\]]*\]|\([^)]*\)|\{[^}]*\}|"[^"]*"|\|[^|]*\|""", RegexOptions.CultureInvariant)]
    private static partial Regex Labels();

    [GeneratedRegex(@"^(<?[-=.]{2,}[>ox]?|~~~)$", RegexOptions.CultureInvariant)]
    private static partial Regex Arrow();
}
