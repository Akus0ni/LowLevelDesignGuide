using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Uml;

/// <summary>A diagram element and the lines of the paired code that make it.</summary>
/// <param name="Key">A class, participant or state name, or a relationship key such as <c>Order*--OrderLine</c>.</param>
public sealed record DiagramLink(string Key, IReadOnlyList<int> Lines);

/// <summary>Where the diagram and the code disagree.</summary>
/// <param name="Line">1-based line of the diagram source.</param>
public sealed record DiagramMismatch(int Line, string Message);

public sealed record DiagramCodeResult(IReadOnlyList<DiagramLink> Links, IReadOnlyList<DiagramMismatch> Mismatches)
{
    private const char Separator = ';';
    private const char Equal = '=';

    /// <summary>The packed <c>links</c> attribute: <c>Order=3-12;Order*--OrderLine=5</c> (spec 008 plan §2.4).</summary>
    public string Format() =>
        string.Join(Separator, Links.Where(l => l.Lines.Count > 0).Select(l => $"{l.Key}{Equal}{LineRanges.Format(l.Lines)}"));
}

/// <summary>
/// Matches a diagram against the code it stands for (spec 008 §3.4): every class, participant or state named in the
/// diagram is a type (or, for states, an enum member) in the code; every inheritance or realization arrow is in the
/// subtype's base list; every association, aggregation or composition has a member of the target's type; every
/// dependency has at least one use. Composition vs aggregation is a design claim and is not judged from code.
/// Returns the link map the reader uses to light up code (only for elements that matched) and every mismatch.
/// </summary>
public static class DiagramCodeLinks
{
    public static DiagramCodeResult For(string diagram, SnippetCodeModel code)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(code);
        return DiagramTypes.Of(diagram) switch
        {
            DiagramType.Class => ForClasses(MermaidClassDiagram.Parse(diagram), code),
            DiagramType.State => ForStates(MermaidStateDiagram.Parse(diagram), code),
            DiagramType.Sequence => ForSequence(MermaidSequenceDiagram.Parse(diagram), code),
            _ => new DiagramCodeResult([], []),
        };
    }

    private static DiagramCodeResult ForClasses(ClassDiagramModel diagram, SnippetCodeModel code)
    {
        var links = new List<DiagramLink>();
        var mismatches = new List<DiagramMismatch>();
        foreach (var name in diagram.Classes)
        {
            if (code.Type(name.Name) is { } type)
            {
                links.Add(new DiagramLink(name.Name, [.. type.Lines.All]));
            }
            else
            {
                mismatches.Add(new(name.Line, $"'{name.Name}' is in the diagram but no type of that name is in the code."));
            }
        }

        foreach (var relationship in diagram.Relationships)
        {
            if (code.Type(relationship.Source) is not { } source || code.Type(relationship.Target) is not { } target)
            {
                continue; // The missing type is already reported.
            }

            var (lines, problem) = Evidence(relationship, source, target);
            if (problem is not null)
            {
                mismatches.Add(new(relationship.Line, $"'{relationship.Key}': {problem}"));
            }
            else
            {
                links.Add(new DiagramLink(relationship.Key, lines));
            }
        }

        return new DiagramCodeResult(links, mismatches);
    }

    /// <summary>The code lines that make a relationship, or why there are none.</summary>
    private static (IReadOnlyList<int> Lines, string? Problem) Evidence(DiagramRelationship relationship, CodeType source, CodeType target)
    {
        switch (relationship.Kind)
        {
            case RelationshipKind.Inheritance or RelationshipKind.Realization:
                var bases = source.Bases.Where(b => b.Name == target.Name).Select(b => b.Line).ToList();
                return bases.Count > 0 ? (bases, null) : ([], $"{source.Name} doesn't list {target.Name} as a base type.");

            case RelationshipKind.Dependency:
                var uses = UsesOf(source, target.Name);
                if (uses.Count == 0 && !relationship.Directed)
                {
                    uses = UsesOf(target, source.Name);
                }

                return uses.Count > 0 ? (uses, null) : ([], $"{source.Name} never uses {target.Name}.");

            default:
                var held = HeldBy(source, target.Name);
                if (held.Count == 0 && !relationship.Directed)
                {
                    held = HeldBy(target, source.Name);
                }

                return held.Count > 0
                    ? (held, null)
                    : ([], relationship.Directed
                        ? $"{source.Name} has no field or property of type {target.Name}."
                        : $"neither {source.Name} nor {target.Name} has a field or property of the other's type.");
        }
    }

    private static List<int> HeldBy(CodeType type, string name) => [.. type.MembersOfType(name).Select(m => m.Line).Distinct()];

    private static List<int> UsesOf(CodeType type, string name) =>
        [.. type.Mentions.Where(m => m.Name == name).Select(m => m.Line).Distinct().Order()];

    private static DiagramCodeResult ForStates(StateDiagramModel diagram, SnippetCodeModel code)
    {
        var links = new List<DiagramLink>();
        var mismatches = new List<DiagramMismatch>();
        foreach (var state in diagram.States)
        {
            if (code.EnumMember(state.Name) is { } line)
            {
                links.Add(new DiagramLink(state.Name, [line]));
            }
            else if (code.Type(state.Name) is { } type)
            {
                links.Add(new DiagramLink(state.Name, [.. type.Lines.All]));
            }
            else
            {
                mismatches.Add(new(state.Line, $"State '{state.Name}' is not an enum member or a type in the code."));
            }
        }

        return new DiagramCodeResult(links, mismatches);
    }

    private static DiagramCodeResult ForSequence(SequenceDiagramModel diagram, SnippetCodeModel code)
    {
        var links = new List<DiagramLink>();
        var mismatches = new List<DiagramMismatch>();
        foreach (var participant in diagram.Participants.Where(p => !p.IsActor))
        {
            if (code.Type(participant.Name) is { } type)
            {
                links.Add(new DiagramLink(participant.Id, [.. type.Lines.All]));
            }
            else
            {
                mismatches.Add(new(participant.Line,
                    $"Participant '{participant.Name}' is not a type in the code (declare people with 'actor')."));
            }
        }

        return new DiagramCodeResult(links, mismatches);
    }
}
