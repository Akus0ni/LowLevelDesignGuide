using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Uml;

/// <summary>
/// Reads the participants of a Mermaid <c>sequenceDiagram</c>, line by line, never throwing: <c>participant</c> and
/// <c>actor</c> lines (with <c>as</c> aliases) and every name a message uses. A participant's
/// <see cref="SequenceParticipant.Name"/> is its alias when it has one (<c>participant SM as SessionManager</c> draws
/// SessionManager), else its id.
/// </summary>
public static partial class MermaidSequenceDiagram
{
    private const string ActorKeyword = "actor";

    public static SequenceDiagramModel Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var participants = new Dictionary<string, SequenceParticipant>(StringComparer.Ordinal);

        foreach (var (text, line) in DiagramTypes.Lines(source))
        {
            if (ParticipantLine().Match(text) is { Success: true } declared)
            {
                var id = declared.Groups["id"].Value;
                var alias = declared.Groups["alias"].Success ? declared.Groups["alias"].Value.Trim().Trim('"') : null;
                var isActor = declared.Groups["kind"].Value == ActorKeyword;
                participants.TryAdd(id, new SequenceParticipant(id, string.IsNullOrEmpty(alias) ? id : alias, isActor, line));
            }
            else if (MessageLine().Match(text) is { Success: true } message)
            {
                foreach (var id in new[] { message.Groups["from"].Value, message.Groups["to"].Value })
                {
                    participants.TryAdd(id, new SequenceParticipant(id, id, IsActor: false, line));
                }
            }
        }

        return new SequenceDiagramModel([.. participants.Values]);
    }

    // participant A, participant SM as SessionManager, actor U as "Learner"
    [GeneratedRegex("""^(?<kind>participant|actor)\s+(?<id>[A-Za-z_]\w*)(?:\s+as\s+(?<alias>.+))?$""")]
    private static partial Regex ParticipantLine();

    // A->>B: text, A-->>+B: text, A-xB: text, A-)B: text
    [GeneratedRegex("""^(?<from>[A-Za-z_]\w*)\s*--?(?:>>|>|x|\))\s*[+-]?\s*(?<to>[A-Za-z_]\w*)\s*:""")]
    private static partial Regex MessageLine();
}
