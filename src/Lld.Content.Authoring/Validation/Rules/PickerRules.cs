using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// A module's "Which pattern?" picker (spec 006 §3.6) is well formed, every answer leads somewhere real, there are no
/// loops, no path asks more than <see cref="MaxDepth"/> questions, and (in <c>pattern</c> modules) every lesson of the
/// module can be picked. Other modules may leave lessons out: not every lesson answers "which one?" (spec 008, CD-003).
/// </summary>
public sealed class PickerRule : IContentRule
{
    public const int MaxDepth = 4;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules.SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceModule module)
    {
        if (module.Manifest?[FrontMatterKeys.Picker] is not { Nested: { } nested } entry)
        {
            yield break;
        }

        var picker = PickerTree.Parse(nested);
        if (picker.Problems.Count > 0)
        {
            // Where answers lead only makes sense once the tree is well formed.
            foreach (var problem in picker.Problems)
            {
                yield return new(module.ManifestPath, problem.Line, IssueCodes.PickerShape, problem.Message);
            }

            yield break;
        }

        var lessons = module.Lessons.Select(l => l.Slug).ToHashSet(StringComparer.Ordinal);
        var mustReachAll = module.Manifest[FrontMatterKeys.Template]?.Scalar == ContentSchemas.PatternTemplate;
        foreach (var issue in new PickerWalk(module.ManifestPath, picker, lessons, mustReachAll).Issues(entry.Line))
        {
            yield return issue;
        }
    }

    /// <summary>Walks the tree from the start question, colouring questions to find loops (depth-first).</summary>
    private sealed class PickerWalk(string path, ParsedPicker picker, IReadOnlySet<string> lessons, bool mustReachAll)
    {
        private readonly Dictionary<string, ParsedQuestion> _questions = picker.Questions.ToDictionary(q => q.Id, StringComparer.Ordinal);
        private readonly HashSet<string> _visiting = new(StringComparer.Ordinal);
        private readonly HashSet<string> _visited = new(StringComparer.Ordinal);
        private readonly HashSet<string> _picked = new(StringComparer.Ordinal);
        private readonly List<ContentIssue> _issues = [];

        public List<ContentIssue> Issues(int pickerLine)
        {
            if (!_questions.TryGetValue(picker.Start, out var start))
            {
                _issues.Add(new(path, picker.StartLine, IssueCodes.PickerUnknownNext, $"'start' names '{picker.Start}', which isn't a question."));
            }
            else
            {
                Visit(start, depth: 1);
            }

            foreach (var question in picker.Questions.Where(q => !_visited.Contains(q.Id)))
            {
                _issues.Add(new(path, question.Line, IssueCodes.PickerUnreachable, $"Question '{question.Id}' is never asked."));
            }

            foreach (var lesson in lessons.Except(_picked).Where(_ => mustReachAll).Order(StringComparer.Ordinal))
            {
                _issues.Add(new(path, pickerLine, IssueCodes.PickerUnreachable, $"No answer picks lesson '{lesson}'."));
            }

            return _issues;
        }

        private void Visit(ParsedQuestion question, int depth)
        {
            _visiting.Add(question.Id);
            if (depth > MaxDepth)
            {
                _issues.Add(new(path, question.Line, IssueCodes.PickerTooDeep,
                    $"'{question.Id}' is question {depth} on its path; keep every path to {MaxDepth} questions."));
            }

            foreach (var answer in question.Answers)
            {
                Follow(answer, depth);
            }

            _visiting.Remove(question.Id);
            _visited.Add(question.Id);
        }

        private void Follow(ParsedAnswer answer, int depth)
        {
            foreach (var pick in answer.Picks)
            {
                if (lessons.Contains(pick))
                {
                    _picked.Add(pick);
                }
                else
                {
                    _issues.Add(new(path, answer.Line, IssueCodes.PickerUnknownLesson, $"'{pick}' isn't a lesson in this module."));
                }
            }

            if (answer.Next is not { } next)
            {
                return;
            }

            if (!_questions.TryGetValue(next, out var target))
            {
                _issues.Add(new(path, answer.Line, IssueCodes.PickerUnknownNext, $"'next' names '{next}', which isn't a question."));
            }
            else if (_visiting.Contains(next))
            {
                _issues.Add(new(path, answer.Line, IssueCodes.PickerCycle, $"'{answer.Label}' leads back to '{next}', a loop."));
            }
            else if (!_visited.Contains(next))
            {
                Visit(target, depth + 1);
            }
        }
    }
}

/// <summary>Look-alike groups (spec 006 §3.7) name 2–3 lessons of the module, once each, and no group is repeated.</summary>
public sealed class LookalikesRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules.SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceModule module)
    {
        if (module.Manifest?[FrontMatterKeys.Lookalikes] is not { Nested: { } nested })
        {
            yield break;
        }

        var parsed = Lookalikes.Parse(nested);
        foreach (var problem in parsed.Problems)
        {
            yield return new(module.ManifestPath, problem.Line, IssueCodes.LookalikeShape, problem.Message);
        }

        var lessons = module.Lessons.Select(l => l.Slug).ToHashSet(StringComparer.Ordinal);
        var groups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in parsed.Entries)
        {
            foreach (var unknown in entry.Lessons.Where(l => !lessons.Contains(l)))
            {
                yield return new(module.ManifestPath, entry.Line, IssueCodes.LookalikeUnknownLesson, $"'{unknown}' isn't a lesson in this module.");
            }

            var distinct = entry.Lessons.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
            if (distinct.Count != entry.Lessons.Count)
            {
                yield return new(module.ManifestPath, entry.Line, IssueCodes.LookalikeDuplicate, "A look-alike names the same lesson twice.");
            }
            else if (!groups.Add(string.Join(' ', distinct)))
            {
                yield return new(module.ManifestPath, entry.Line, IssueCodes.LookalikeDuplicate,
                    $"The look-alike {string.Join(" · ", entry.Lessons)} is already listed.");
            }
        }
    }
}
