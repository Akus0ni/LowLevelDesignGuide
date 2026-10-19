using Lld.Contracts;
using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line of the entry in the file.</param>
public sealed record ParsedLookalike(int Line, IReadOnlyList<string> Lessons, string Difference, string Ask)
{
    public Lookalike ToContract() => new(Lessons, Difference, Ask);
}

public sealed record ParsedLookalikes(IReadOnlyList<ParsedLookalike> Entries, IReadOnlyList<LineProblem> Problems);

/// <summary>
/// Reads look-alike groups (spec 006 §3.7) from <c>module.yml</c>:
/// <code>
/// lookalikes:
///   - patterns: [strategy, state]
///     difference: "Strategy is chosen by the caller; State switches itself."
///     ask: "Who swaps the behaviour: the client or the object itself?"
/// </code>
/// Only the shape is checked here; which lessons exist is checked by the look-alikes rule.
/// </summary>
public static class Lookalikes
{
    public const int MinLessons = 2;
    public const int MaxLessons = 3;
    public const int MaxTextLength = 160;

    private const string PatternsKey = "patterns";
    private const string DifferenceKey = "difference";
    private const string AskKey = "ask";

    private static readonly string[] Keys = [PatternsKey, DifferenceKey, AskKey];

    public static ParsedLookalikes Parse(YamlNested nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        var problems = new List<LineProblem>();
        if (nested.Root is not YamlSequenceNode list)
        {
            problems.Add(new(nested.LineOf(nested.Root), "Write look-alikes as a list of '- patterns: [a, b]' entries."));
            return new ParsedLookalikes([], problems);
        }

        var entries = list.Children.Select(item => Entry(nested, item, problems)).OfType<ParsedLookalike>().ToList();
        return new ParsedLookalikes(entries, problems);
    }

    private static ParsedLookalike? Entry(YamlNested nested, YamlNode item, List<LineProblem> problems)
    {
        var line = nested.LineOf(item);
        if (item is not YamlMappingNode entry)
        {
            problems.Add(new(line, "Each look-alike needs 'patterns:', 'difference:' and 'ask:'."));
            return null;
        }

        foreach (var unknown in YamlNodes.Keys(entry).Where(k => !Keys.Contains(k, StringComparer.Ordinal)))
        {
            problems.Add(new(line, $"Unknown look-alike key '{unknown}'. Allowed: {string.Join(", ", Keys)}."));
        }

        var lessons = YamlNodes.Items(entry, PatternsKey);
        if (lessons.Count is < MinLessons or > MaxLessons)
        {
            problems.Add(new(line, $"'{PatternsKey}' lists {MinLessons}–{MaxLessons} lessons; found {lessons.Count}."));
        }

        var difference = Text(entry, DifferenceKey, line, problems);
        var ask = Text(entry, AskKey, line, problems);
        return new ParsedLookalike(line, lessons, difference, ask);
    }

    private static string Text(YamlMappingNode entry, string key, int line, List<LineProblem> problems)
    {
        var text = YamlNodes.Scalar(entry, key);
        if (text is null)
        {
            problems.Add(new(line, $"Add '{key}:'."));
            return string.Empty;
        }

        if (text.Length > MaxTextLength)
        {
            problems.Add(new(line, $"'{key}' is {text.Length} characters; keep it to {MaxTextLength}."));
        }

        return text;
    }
}
