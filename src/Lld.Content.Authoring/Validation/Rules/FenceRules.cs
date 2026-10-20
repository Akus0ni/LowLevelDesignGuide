using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Snippets;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// A code fence's info string uses only known flags and attributes, with a valid <c>mark</c>; the diagram keys
/// (<c>code</c>, <c>steps</c>, spec 008) sit on <c>mermaid</c> fences only, and <c>steps</c> on flowcharts only.
/// </summary>
public sealed class FenceInfoRule : IContentRule
{
    private const string Mermaid = "mermaid";
    private static readonly string[] FlowchartStarts = ["flowchart", "graph"];

    private static readonly HashSet<string> Flags = [FenceInfo.BeforeFlag, FenceInfo.AfterFlag, FenceInfo.StepsFlag];
    private static readonly HashSet<string> Attributes =
        [FenceInfo.SnippetAttribute, FenceInfo.TitleAttribute, FenceInfo.MarkAttribute, FenceInfo.CodeAttribute];

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from fence in CodeFences.Of(document)
        from issue in Problems(fence)
        select new ContentIssue(document.Path, fence.Line, issue.Code, issue.Message);

    private static IEnumerable<(string Code, string Message)> Problems(CodeFence fence)
    {
        var info = fence.Info;
        foreach (var problem in info.Problems)
        {
            yield return (IssueCodes.FenceSyntax, problem);
        }

        foreach (var flag in info.Flags.Where(f => !Flags.Contains(f)))
        {
            yield return (IssueCodes.FenceUnknownFlag, $"Unknown flag '{flag}'. Allowed: {string.Join(", ", Flags)}.");
        }

        foreach (var attribute in info.Attributes.Where(a => !Attributes.Contains(a.Name)))
        {
            yield return (IssueCodes.FenceUnknownAttribute, $"Unknown attribute '{attribute.Name}'. Allowed: {string.Join(", ", Attributes)}.");
        }

        foreach (var problem in DiagramKeyProblems(fence))
        {
            yield return problem;
        }

        if (info[FenceInfo.MarkAttribute] is not { } mark)
        {
            yield break;
        }

        if (info.IsBefore || info.IsAfter)
        {
            yield return (IssueCodes.FenceMarkOnCompare, "Before / After fences get their marks from the diff; remove 'mark'.");
        }
        else if (LineRanges.Parse(mark) is null)
        {
            yield return (IssueCodes.FenceBadMark, $"mark=\"{mark}\" must be line numbers or ranges, e.g. mark=\"3-5,9\".");
        }
    }

    private static IEnumerable<(string Code, string Message)> DiagramKeyProblems(CodeFence fence)
    {
        var info = fence.Info;
        var isMermaid = string.Equals(info.Language, Mermaid, StringComparison.OrdinalIgnoreCase);
        if (!isMermaid && info[FenceInfo.CodeAttribute] is not null)
        {
            yield return (IssueCodes.FenceDiagramKeyNotMermaid, "'code' names the C# a diagram draws: use it on a ```mermaid fence.");
        }

        if (!info.HasFlag(FenceInfo.StepsFlag))
        {
            yield break;
        }

        var first = fence.Code.TrimStart().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!isMermaid || !FlowchartStarts.Contains(first, StringComparer.Ordinal))
        {
            yield return (IssueCodes.FenceStepsNotFlowchart, "'steps' walks an activity diagram: use it on a ```mermaid fence that starts with 'flowchart'.");
        }
    }
}

/// <summary>A snippet fence quotes C# and has no code of its own (the code comes from the file).</summary>
public sealed class SnippetFenceRule : IContentRule
{
    private static readonly HashSet<string> CSharp = new(StringComparer.OrdinalIgnoreCase) { "csharp", "cs", "c#" };

    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        foreach (var document in context.Documents)
        {
            foreach (var fence in CodeFences.Of(document).Where(f => f.Snippet is not null))
            {
                if (!CSharp.Contains(fence.Info.Language))
                {
                    yield return new(document.Path, fence.Line, IssueCodes.SnippetNotCSharp, $"Snippets quote C#: start the fence with ```csharp, not '{fence.Info.Language}'.");
                }

                if (!string.IsNullOrWhiteSpace(fence.Code))
                {
                    yield return new(document.Path, fence.Line, IssueCodes.SnippetHasCode, "A snippet fence must be empty: its code comes from the quoted file.");
                }
            }
        }
    }
}

/// <summary>Every snippet points at a file that exists and a type or member that is in it, exactly once.</summary>
public sealed class SnippetResolvesRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from fence in CodeFences.Of(document)
        where fence.Snippet is not null
        let problem = Problem(context.Snippets, fence.Snippet!)
        where problem is not null
        select new ContentIssue(document.Path, fence.Line, problem.Value.Code, problem.Value.Message);

    private static (string Code, string Message)? Problem(ICodeSnippetSource snippets, string value)
    {
        var (reference, problem) = SnippetReference.Parse(value);
        if (reference is null)
        {
            return (IssueCodes.SnippetBadReference, problem!);
        }

        return snippets.Extract(reference) switch
        {
            SnippetResult.FileMissing missing => (IssueCodes.SnippetFileMissing, $"'{value}': {missing.Path} doesn't exist."),
            SnippetResult.SymbolMissing symbol => (IssueCodes.SnippetSymbolMissing, $"'{value}': no type or member '{symbol.Selector}' in {reference.Path}."),
            SnippetResult.Ambiguous ambiguous =>
                (IssueCodes.SnippetSymbolAmbiguous, $"'{value}': '{ambiguous.Selector}' matches {ambiguous.Count} declarations; qualify it (Outer.Inner, Box<T>)."),
            _ => null,
        };
    }
}

/// <summary>Each <c>before</c> fence is followed directly by an <c>after</c> fence in the same language, and the reverse.</summary>
public sealed class ComparePairRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        foreach (var document in context.Documents)
        {
            var layout = CodeFences.Compare(CodeFences.Of(document));
            foreach (var fence in layout.Unpaired)
            {
                yield return new(document.Path, fence.Line, IssueCodes.CompareUnpaired,
                    "A 'before' fence needs an 'after' fence right below it (no text in between), and the reverse.");
            }

            foreach (var pair in layout.Pairs.Where(p => !string.Equals(p.Before.Info.Language, p.After.Info.Language, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new(document.Path, pair.After.Line, IssueCodes.CompareLanguageMismatch,
                    $"Before is '{pair.Before.Info.Language}' but After is '{pair.After.Info.Language}'.");
            }
        }
    }
}
