using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Uml;

/// <summary>
/// Reads the classes and relationships of a Mermaid <c>classDiagram</c>, line by line, never throwing: only what the
/// Code ↔ UML check needs (spec 008 §3.4). Generic names (<c>Box~T~</c>) count as their plain name (<c>Box</c>);
/// members, notes, styles and directions are skipped.
/// </summary>
public static partial class MermaidClassDiagram
{
    private const string Generic = "~";

    public static ClassDiagramModel Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var classes = new Dictionary<string, DiagramName>(StringComparer.Ordinal);
        var relationships = new List<DiagramRelationship>();
        var depth = 0;

        void Declare(string raw, int line)
        {
            var name = PlainName(raw);
            classes.TryAdd(name, new DiagramName(name, line));
        }

        foreach (var (text, line) in DiagramTypes.Lines(source))
        {
            // Inside a class body: members only, until the closing brace.
            if (depth > 0)
            {
                depth += Count(text, '{') - Count(text, '}');
                continue;
            }

            if (RelationshipLine().Match(text) is { Success: true } relationship)
            {
                Declare(relationship.Groups["a"].Value, line);
                Declare(relationship.Groups["b"].Value, line);
                relationships.Add(Relationship(relationship, line));
            }
            else if (ClassLine().Match(text) is { Success: true } declaration)
            {
                Declare(declaration.Groups["name"].Value, line);
                depth = Count(text, '{') - Count(text, '}');
            }
            else if (AnnotationLine().Match(text) is { Success: true } annotation)
            {
                Declare(annotation.Groups["name"].Value, line);
            }
            else if (MemberLine().Match(text) is { Success: true } member)
            {
                Declare(member.Groups["name"].Value, line);
            }
        }

        return new ClassDiagramModel([.. classes.Values], relationships);
    }

    /// <summary><c>Box~T~</c> → <c>Box</c>.</summary>
    public static string PlainName(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var generic = raw.IndexOf(Generic, StringComparison.Ordinal);
        return generic < 0 ? raw : raw[..generic];
    }

    private static DiagramRelationship Relationship(Match match, int line)
    {
        var a = PlainName(match.Groups["a"].Value);
        var b = PlainName(match.Groups["b"].Value);
        var left = match.Groups["left"].Value;
        var right = match.Groups["right"].Value;
        var dashed = match.Groups["body"].Value == "..";
        var key = $"{a}{left}{match.Groups["body"].Value}{right}{b}";

        // The head marks the target for <|, |>, < and >; the diamond (* or o) marks the whole.
        return (left, right) switch
        {
            ("<|", _) => new(b, a, dashed ? RelationshipKind.Realization : RelationshipKind.Inheritance, true, key, line),
            (_, "|>") => new(a, b, dashed ? RelationshipKind.Realization : RelationshipKind.Inheritance, true, key, line),
            ("*", _) => new(a, b, RelationshipKind.Composition, true, key, line),
            (_, "*") => new(b, a, RelationshipKind.Composition, true, key, line),
            ("o", _) => new(a, b, RelationshipKind.Aggregation, true, key, line),
            (_, "o") => new(b, a, RelationshipKind.Aggregation, true, key, line),
            ("<", _) => new(b, a, dashed ? RelationshipKind.Dependency : RelationshipKind.Association, true, key, line),
            (_, ">") => new(a, b, dashed ? RelationshipKind.Dependency : RelationshipKind.Association, true, key, line),
            _ => new(a, b, dashed ? RelationshipKind.Dependency : RelationshipKind.Association, false, key, line),
        };
    }

    private static int Count(string text, char c) => text.Count(x => x == c);

    // A "1" *-- "many" B : label. Whitespace or a cardinality separates the names from the arrow.
    [GeneratedRegex("""^(?<a>[A-Za-z_][\w~]*)\s*(?:"[^"]*"\s*)?(?<!\w)(?<left><\||\*|o|<)?(?<body>--|\.\.)(?<right>\|>|\*|o|>)?(?!\w)\s*(?:"[^"]*"\s*)?(?<b>[A-Za-z_][\w~]*)\s*(?::.*)?$""")]
    private static partial Regex RelationshipLine();

    // class Order, class Order {, class Box~T~, class Order["Label"], class Order:::style
    [GeneratedRegex("""^class\s+(?<name>[A-Za-z_][\w~]*)""")]
    private static partial Regex ClassLine();

    // <<interface>> IShape
    [GeneratedRegex("""^<<[^>]+>>\s+(?<name>[A-Za-z_][\w~]*)\s*$""")]
    private static partial Regex AnnotationLine();

    // Order : +Add(line)
    [GeneratedRegex("""^(?<name>[A-Za-z_][\w~]*)\s*:\s*\S""")]
    private static partial Regex MemberLine();
}
