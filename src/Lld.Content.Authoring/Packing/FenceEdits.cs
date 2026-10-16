using System.Text;
using Lld.Content.Authoring.Editing;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Snippets;

namespace Lld.Content.Authoring.Packing;

/// <summary>
/// Rewrites code fences for the reader (spec 004 §3.3–3.4): a snippet fence gets the code it quotes and loses its
/// <c>snippet</c> attribute; a Before / After pair gets <c>mark</c> attributes for the lines that changed, when marks
/// help (<see cref="LineMarks"/>). Other fences
/// are left exactly as written. Runs only on validated content: an unresolved snippet is a bug, not an author error.
/// </summary>
public sealed class FenceEdits(ICodeSnippetSource snippets) : IBodyEditSource
{
    private const char LineFeed = '\n';
    private const char Backtick = '`';
    private const int MinimumFence = 3;

    public IEnumerable<TextEdit> EditsFor(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var fences = CodeFences.Of(source);
        var marks = Marks(CodeFences.Compare(fences), source.Path);
        foreach (var fence in fences)
        {
            var hasMarks = marks.TryGetValue(fence, out var lines);
            if (fence.Snippet is null && !hasMarks)
            {
                continue;
            }

            var info = fence.Info.Without(FenceInfo.SnippetAttribute);
            if (hasMarks)
            {
                info = lines!.Count > 0 ? info.With(FenceInfo.MarkAttribute, LineRanges.Format(lines)) : info.Without(FenceInfo.MarkAttribute);
            }

            yield return Rewrite(source, fence, info, CodeOf(fence, source.Path));
        }
    }

    private Dictionary<CodeFence, IReadOnlyList<int>> Marks(CompareLayout layout, string path)
    {
        var marks = new Dictionary<CodeFence, IReadOnlyList<int>>();
        foreach (var pair in layout.Pairs)
        {
            var before = CodeOf(pair.Before, path);
            var after = CodeOf(pair.After, path);
            var changes = LineDiff.Compare(before, after);
            marks[pair.Before] = LineMarks.ForDisplay(changes.Removed, before);
            marks[pair.After] = LineMarks.ForDisplay(changes.Added, after);
        }

        return marks;
    }

    private string CodeOf(CodeFence fence, string path)
    {
        if (fence.Snippet is not { } value)
        {
            return fence.Code;
        }

        var (reference, problem) = SnippetReference.Parse(value);
        return reference is not null && snippets.Extract(reference) is SnippetResult.Found found
            ? found.Code
            : throw new InvalidOperationException($"{path}:{fence.Line}: snippet '{value}' doesn't resolve ({problem ?? "see validation"}); validate before packing.");
    }

    /// <summary>
    /// The whole fence, written again. The first line keeps whatever came before the fence on its line (list marker,
    /// <c>&gt;</c>); later lines repeat it with list markers turned into spaces, so the fence stays in its container.
    /// </summary>
    private static TextEdit Rewrite(SourceDocument source, CodeFence fence, FenceInfo info, string code)
    {
        var start = fence.Block.Span.Start;
        var lineStart = source.Text.LastIndexOf(LineFeed, Math.Max(0, start - 1)) + 1;
        var prefix = string.Concat(source.Text[lineStart..start].Select(c => c == '>' ? c : ' '));
        var marker = new string(Backtick, Math.Max(MinimumFence, LongestBacktickRun(code) + 1));
        var text = new StringBuilder().Append(marker).Append(info).Append(LineFeed);
        foreach (var line in code.Split(LineFeed))
        {
            text.Append(line.Length == 0 ? prefix.TrimEnd() : prefix + line).Append(LineFeed);
        }

        text.Append(prefix).Append(marker);
        return new TextEdit(start, fence.Block.Span.Length, text.ToString());
    }

    private static int LongestBacktickRun(string code)
    {
        int longest = 0, current = 0;
        foreach (var c in code)
        {
            current = c == Backtick ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }
}
