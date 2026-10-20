using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Uml;

/// <summary>
/// Reads the states and transitions of a Mermaid <c>stateDiagram-v2</c>, line by line, never throwing. <c>[*]</c> is the
/// start or end pseudo-state, not a state; <c>state "Long name" as Id</c> and composite <c>state Id {</c> declare
/// <c>Id</c>. Notes, styles and directions are skipped.
/// </summary>
public static partial class MermaidStateDiagram
{
    public const string Pseudo = "[*]";

    public static StateDiagramModel Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var states = new Dictionary<string, DiagramName>(StringComparer.Ordinal);
        var transitions = new List<StateTransition>();

        void Declare(string name, int line)
        {
            if (name != Pseudo)
            {
                states.TryAdd(name, new DiagramName(name, line));
            }
        }

        foreach (var (text, line) in DiagramTypes.Lines(source))
        {
            if (TransitionLine().Match(text) is { Success: true } transition)
            {
                var from = transition.Groups["from"].Value;
                var to = transition.Groups["to"].Value;
                Declare(from, line);
                Declare(to, line);
                var label = transition.Groups["event"].Success ? transition.Groups["event"].Value.Trim() : null;
                transitions.Add(new StateTransition(from, to, string.IsNullOrEmpty(label) ? null : label, line));
            }
            else if (StateLine().Match(text) is { Success: true } state)
            {
                Declare(state.Groups["id"].Value, line);
            }
            else if (DescriptionLine().Match(text) is { Success: true } description)
            {
                Declare(description.Groups["id"].Value, line);
            }
        }

        return new StateDiagramModel([.. states.Values], transitions);
    }

    // A --> B, [*] --> A, A --> [*] : done
    [GeneratedRegex("""^(?<from>\[\*\]|[A-Za-z_]\w*)\s*-->\s*(?<to>\[\*\]|[A-Za-z_]\w*)\s*(?::(?<event>.*))?$""")]
    private static partial Regex TransitionLine();

    // state Pending, state "Waiting for review" as Pending, state Pending {, state fork1 <<fork>>
    [GeneratedRegex("""^state\s+(?:"[^"]*"\s+as\s+)?(?<id>[A-Za-z_]\w*)""")]
    private static partial Regex StateLine();

    // Pending : Waiting for an admin
    [GeneratedRegex("""^(?<id>[A-Za-z_]\w*)\s*:\s*\S""")]
    private static partial Regex DescriptionLine();
}
