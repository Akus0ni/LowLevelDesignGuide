using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <summary>A fenced code block with its parsed info string.</summary>
/// <param name="Line">1-based line of the opening fence.</param>
/// <param name="Code">The lines between the fences, joined with <c>\n</c>.</param>
public sealed record CodeFence(FencedCodeBlock Block, FenceInfo Info, int Line, string Code)
{
    public string? Snippet => Info[FenceInfo.SnippetAttribute];
}

/// <summary>A <c>before</c> fence and the <c>after</c> fence right below it (spec 004 §3.4).</summary>
public sealed record ComparePair(CodeFence Before, CodeFence After);

/// <param name="Unpaired">Fences flagged <c>before</c> or <c>after</c> that have no partner.</param>
public sealed record CompareLayout(IReadOnlyList<ComparePair> Pairs, IReadOnlyList<CodeFence> Unpaired);

public static class CodeFences
{
    /// <summary>Every fenced code block, in document order (diagrams included).</summary>
    public static IReadOnlyList<CodeFence> Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return [.. source.Document.Descendants<FencedCodeBlock>().Select(block => new CodeFence(
            block,
            FenceInfo.Parse($"{block.Info} {block.Arguments}"),
            source.LineAt(block.Span.Start),
            block.Lines.ToString()))];
    }

    /// <summary>
    /// Pairs each <c>before</c> fence with an <c>after</c> fence that is the very next block in the same container.
    /// A fence flagged both ways is never paired.
    /// </summary>
    public static CompareLayout Compare(IReadOnlyList<CodeFence> fences)
    {
        ArgumentNullException.ThrowIfNull(fences);
        var pairs = new List<ComparePair>();
        var unpaired = new List<CodeFence>();
        for (var i = 0; i < fences.Count; i++)
        {
            var fence = fences[i];
            if (!fence.Info.IsBefore && !fence.Info.IsAfter)
            {
                continue;
            }

            if (i + 1 < fences.Count && IsPair(fence, fences[i + 1]))
            {
                pairs.Add(new ComparePair(fence, fences[++i]));
            }
            else
            {
                unpaired.Add(fence);
            }
        }

        return new CompareLayout(pairs, unpaired);
    }

    private static bool IsPair(CodeFence before, CodeFence after) =>
        before.Info is { IsBefore: true, IsAfter: false }
        && after.Info is { IsAfter: true, IsBefore: false }
        && before.Block.Parent is { } parent
        && ReferenceEquals(parent, after.Block.Parent)
        && parent.IndexOf(after.Block) == parent.IndexOf(before.Block) + 1;
}
