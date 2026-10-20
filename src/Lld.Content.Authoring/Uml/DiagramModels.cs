namespace Lld.Content.Authoring.Uml;

/// <summary>A name a diagram draws (a class, participant or state) and the 1-based line of the diagram source it is on.</summary>
public sealed record DiagramName(string Name, int Line);

/// <summary>The six relationships of a UML class diagram (spec 008 §3.5), plus a plain link with no arrowhead.</summary>
public enum RelationshipKind
{
    Association,
    Aggregation,
    Composition,
    Inheritance,
    Realization,
    Dependency,
}

/// <summary>
/// One relationship as drawn. <see cref="Source"/> is the side that owns, uses or derives (the whole, the subtype,
/// the user); <see cref="Target"/> is the part, the supertype or what is used. For a plain link (<c>--</c> or
/// <c>..</c>) <see cref="Directed"/> is false and either side may hold the other.
/// </summary>
/// <param name="Key">As written, without spaces or cardinalities, e.g. <c>Order*--OrderLine</c>; the reader matches
/// edges by this key (spec 008 plan §2.4).</param>
/// <param name="Line">1-based line of the diagram source.</param>
public sealed record DiagramRelationship(string Source, string Target, RelationshipKind Kind, bool Directed, string Key, int Line);

public sealed record ClassDiagramModel(IReadOnlyList<DiagramName> Classes, IReadOnlyList<DiagramRelationship> Relationships);

/// <param name="Event">The transition label, e.g. <c>approve</c>; <c>null</c> when unlabelled.</param>
public sealed record StateTransition(string From, string To, string? Event, int Line);

/// <param name="States">Every named state; the start / end pseudo-state <c>[*]</c> is not one.</param>
public sealed record StateDiagramModel(IReadOnlyList<DiagramName> States, IReadOnlyList<StateTransition> Transitions);

/// <param name="IsActor">Declared with <c>actor</c>: a person, so no code stands for it.</param>
public sealed record SequenceParticipant(string Id, string Name, bool IsActor, int Line);

public sealed record SequenceDiagramModel(IReadOnlyList<SequenceParticipant> Participants);

/// <summary>The kind of diagram a Mermaid source draws, from its first word.</summary>
public enum DiagramType
{
    Other,
    Class,
    State,
    Sequence,
}

public static class DiagramTypes
{
    public static DiagramType Of(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var first = source.TrimStart().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first switch
        {
            "classDiagram" => DiagramType.Class,
            "stateDiagram" or "stateDiagram-v2" => DiagramType.State,
            "sequenceDiagram" => DiagramType.Sequence,
            _ => DiagramType.Other,
        };
    }

    /// <summary>The lines of a diagram source, numbered from 1, with Mermaid <c>%%</c> comments removed.</summary>
    internal static IEnumerable<(string Text, int Line)> Lines(string source) =>
        source.ReplaceLineEndings("\n").Split('\n')
            .Select((text, i) => (Text: StripComment(text).Trim(), Line: i + 1))
            .Where(l => l.Text.Length > 0);

    private static string StripComment(string line)
    {
        var comment = line.IndexOf("%%", StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }
}
