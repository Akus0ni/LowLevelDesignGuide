using Lld.Contracts;
using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

/// <summary>A problem with where a <c>module.yml</c> value was written.</summary>
/// <param name="Line">1-based line in the file.</param>
public sealed record LineProblem(int Line, string Message);

/// <param name="Line">1-based line of the answer in the file.</param>
public sealed record ParsedAnswer(string Label, int Line, string? Next, IReadOnlyList<string> Picks, string? Why);

/// <param name="Line">1-based line of the question's id in the file.</param>
public sealed record ParsedQuestion(string Id, int Line, string Ask, IReadOnlyList<ParsedAnswer> Answers);

/// <summary>A picker as written, with lines for the rule (<see cref="PickerTree"/>) and a contract for the pack.</summary>
public sealed record ParsedPicker(string Start, int StartLine, IReadOnlyList<ParsedQuestion> Questions, IReadOnlyList<LineProblem> Problems)
{
    /// <summary>Optional button and sheet title (spec 008, CD-005).</summary>
    public string? Title { get; init; }

    public ModulePicker ToContract() => new(
        Start,
        [.. Questions.Select(q => new PickerQuestion(q.Id, q.Ask, [.. q.Answers.Select(a => new PickerAnswer(a.Label, a.Next, a.Picks, a.Why))]))],
        Title);
}

/// <summary>
/// Reads the "Which pattern?" decision tree (spec 006 §3.6) from <c>module.yml</c>; <c>title</c> is optional (spec 008):
/// <code>
/// picker:
///   title: "Which diagram?"
///   start: what
///   questions:
///     what:
///       ask: "What is your problem about?"
///       answers:
///         - { label: "Creating objects", next: create }
///         - { label: "Swap an algorithm", pick: [strategy], why: "The caller chooses how." }
/// </code>
/// Only the shape is checked here (each problem with its line); where answers lead is checked by the picker rule.
/// </summary>
public static class PickerTree
{
    public const int MinAnswers = 2;
    public const int MaxAnswers = 5;
    public const int MaxPicks = 2;
    public const int MaxTitleLength = 40;

    private const string StartKey = "start";
    private const string TitleKey = "title";
    private const string QuestionsKey = "questions";
    private const string AskKey = "ask";
    private const string AnswersKey = "answers";
    private const string LabelKey = "label";
    private const string NextKey = "next";
    private const string PickKey = "pick";
    private const string WhyKey = "why";

    private static readonly string[] AnswerKeys = [LabelKey, NextKey, PickKey, WhyKey];

    public static ParsedPicker Parse(YamlNested nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        var problems = new List<LineProblem>();
        if (nested.Root is not YamlMappingNode root)
        {
            problems.Add(new(nested.LineOf(nested.Root), "Write the picker as 'start:' and 'questions:'."));
            return new ParsedPicker(string.Empty, nested.LineOf(nested.Root), [], problems);
        }

        var start = YamlNodes.Scalar(root, StartKey);
        if (start is null)
        {
            problems.Add(new(nested.LineOf(root), "Add 'start:', the id of the first question."));
        }

        var questions = new List<ParsedQuestion>();
        if (YamlNodes.Child(root, QuestionsKey) is not YamlMappingNode questionMap)
        {
            problems.Add(new(nested.LineOf(root), "Add 'questions:', a map from question id to 'ask' and 'answers'."));
        }
        else
        {
            questions.AddRange(questionMap.Children.Select(pair => Question(nested, pair.Key, pair.Value, problems)));
        }

        var title = YamlNodes.Scalar(root, TitleKey);
        if (title is not null && (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength))
        {
            var titleLine = YamlNodes.Child(root, TitleKey) is { } titleNode ? nested.LineOf(titleNode) : nested.LineOf(root);
            problems.Add(new(titleLine, $"'title' is the picker button's text: 1–{MaxTitleLength} characters, e.g. \"Which diagram?\"."));
        }

        var startLine = YamlNodes.Child(root, StartKey) is { } startNode ? nested.LineOf(startNode) : nested.LineOf(root);
        return new ParsedPicker(start ?? string.Empty, startLine, questions, problems) { Title = title };
    }

    private static ParsedQuestion Question(YamlNested nested, YamlNode key, YamlNode value, List<LineProblem> problems)
    {
        var id = (key as YamlScalarNode)?.Value ?? string.Empty;
        var line = nested.LineOf(key);
        if (value is not YamlMappingNode question)
        {
            problems.Add(new(line, $"Question '{id}' needs 'ask:' and 'answers:'."));
            return new ParsedQuestion(id, line, string.Empty, []);
        }

        var ask = YamlNodes.Scalar(question, AskKey);
        if (string.IsNullOrWhiteSpace(ask))
        {
            problems.Add(new(line, $"Question '{id}' needs 'ask:', the question shown to the learner."));
        }

        var answers = new List<ParsedAnswer>();
        if (YamlNodes.Child(question, AnswersKey) is not YamlSequenceNode list)
        {
            problems.Add(new(line, $"Question '{id}' needs 'answers:', a list."));
        }
        else
        {
            if (list.Children.Count is < MinAnswers or > MaxAnswers)
            {
                problems.Add(new(nested.LineOf(list), $"Question '{id}' has {list.Children.Count} answers; give {MinAnswers}–{MaxAnswers}."));
            }

            answers.AddRange(list.Children.Select(item => Answer(nested, id, item, problems)).OfType<ParsedAnswer>());
        }

        return new ParsedQuestion(id, line, ask ?? string.Empty, answers);
    }

    private static ParsedAnswer? Answer(YamlNested nested, string questionId, YamlNode item, List<LineProblem> problems)
    {
        var line = nested.LineOf(item);
        if (item is not YamlMappingNode answer)
        {
            problems.Add(new(line, $"An answer of '{questionId}' must be a map like {{ label: \"...\", next: id }}."));
            return null;
        }

        foreach (var unknown in YamlNodes.Keys(answer).Where(k => !AnswerKeys.Contains(k, StringComparer.Ordinal)))
        {
            problems.Add(new(line, $"Unknown answer key '{unknown}'. Allowed: {string.Join(", ", AnswerKeys)}."));
        }

        var label = YamlNodes.Scalar(answer, LabelKey);
        var next = YamlNodes.Scalar(answer, NextKey);
        var picks = YamlNodes.Items(answer, PickKey);
        var why = YamlNodes.Scalar(answer, WhyKey);
        if (string.IsNullOrWhiteSpace(label))
        {
            problems.Add(new(line, $"An answer of '{questionId}' needs 'label:'."));
        }

        if ((next is null) == (picks.Count == 0))
        {
            problems.Add(new(line, $"Answer '{label}' needs either 'next:' (a question) or 'pick:' (lessons), not both."));
        }
        else if (picks.Count > MaxPicks)
        {
            problems.Add(new(line, $"Answer '{label}' picks {picks.Count} lessons; pick 1–{MaxPicks}."));
        }
        else if (picks.Count > 0 && string.IsNullOrWhiteSpace(why))
        {
            problems.Add(new(line, $"Answer '{label}' needs 'why:', one line on why these lessons fit."));
        }

        return new ParsedAnswer(label ?? string.Empty, line, next, picks, why);
    }
}
