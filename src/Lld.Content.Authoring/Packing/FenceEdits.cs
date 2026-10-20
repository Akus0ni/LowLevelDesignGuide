using System.Text;
using Lld.Content.Authoring.Editing;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Snippets;
using Lld.Content.Authoring.Uml;

namespace Lld.Content.Authoring.Packing;

/// <summary>
/// Rewrites code fences for the reader (spec 004 §3.3–3.4): a snippet fence gets the code it quotes and loses its
/// <c>snippet</c> attribute; a Before / After pair gets <c>mark</c> attributes for the lines that changed, when marks
/// help (<see cref="LineMarks"/>); a Code ↔ UML diagram (<c>mermaid code="…"</c>, spec 008 §3.4) becomes
/// <c>mermaid pair links="…"</c> followed by a <c>csharp paired</c> fence holding the code it draws. Other fences
/// are left exactly as written. Runs only on validated content: an unresolved snippet is a bug, not an author error.
/// </summary>
public sealed class FenceEdits(ICodeSnippetSource snippets) : IBodyEditSource
{
    private const char LineFeed = '\n';
    private const char Backtick = '`';
    private const int MinimumFence = 3;
    private const string CSharp = "csharp";

    public IEnumerable<TextEdit> EditsFor(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var fences = CodeFences.Of(source);
        var marks = Marks(CodeFences.Compare(fences), source.Path);
        foreach (var fence in fences)
        {
            if (fence.Info[FenceInfo.CodeAttribute] is { } pairedCode && IsMermaid(fence))
            {
                yield return Pair(source, fence, pairedCode);
                continue;
            }

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

    /// <summary>The diagram with its link map, then the code it draws, in one edit.</summary>
    private TextEdit Pair(SourceDocument source, CodeFence diagram, string reference)
    {
        var code = Quote(reference, diagram, source.Path);
        var links = DiagramCodeLinks.For(diagram.Code, SnippetCodeModel.Of(code)).Format();
        var info = diagram.Info.Without(FenceInfo.CodeAttribute).WithFlag(FenceInfo.PairFlag);
        info = links.Length > 0 ? info.With(FenceInfo.LinksAttribute, links) : info;
        var prefix = PrefixOf(source, diagram);
        var text = new StringBuilder(FenceText(prefix, info, diagram.Code))
            .Append(LineFeed).Append(prefix)
            .Append(FenceText(prefix, FenceInfo.Parse($"{CSharp} {FenceInfo.PairedFlag}"), code));
        return new TextEdit(diagram.Block.Span.Start, diagram.Block.Span.Length, text.ToString());
    }

    private static bool IsMermaid(CodeFence fence) => string.Equals(fence.Info.Language, "mermaid", StringComparison.OrdinalIgnoreCase);

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

    private string CodeOf(CodeFence fence, string path) =>
        fence.Snippet is { } value ? Quote(value, fence, path) : fence.Code;

    private string Quote(string value, CodeFence fence, string path)
    {
        var (reference, problem) = SnippetReference.Parse(value);
        return reference is not null && snippets.Extract(reference) is SnippetResult.Found found
            ? found.Code
            : throw new InvalidOperationException($"{path}:{fence.Line}: snippet '{value}' doesn't resolve ({problem ?? "see validation"}); validate before packing.");
    }

    /// <summary>
    /// The whole fence, written again. The first line keeps whatever came before the fence on its line (list marker,
    /// <c>&gt;</c>); later lines repeat it with list markers turned into spaces, so the fence stays in its container.
    /// </summary>
    private static TextEdit Rewrite(SourceDocument source, CodeFence fence, FenceInfo info, string code) =>
        new(fence.Block.Span.Start, fence.Block.Span.Length, FenceText(PrefixOf(source, fence), info, code));

    /// <summary>What comes before the fence on its line, with list markers turned into spaces.</summary>
    private static string PrefixOf(SourceDocument source, CodeFence fence)
    {
        var start = fence.Block.Span.Start;
        var lineStart = source.Text.LastIndexOf(LineFeed, Math.Max(0, start - 1)) + 1;
        return string.Concat(source.Text[lineStart..start].Select(c => c == '>' ? c : ' '));
    }

    /// <summary>A fence from its opening marker to its closing one; every line after the first starts with the prefix.</summary>
    private static string FenceText(string prefix, FenceInfo info, string code)
    {
        var marker = new string(Backtick, Math.Max(MinimumFence, LongestBacktickRun(code) + 1));
        var text = new StringBuilder().Append(marker).Append(info).Append(LineFeed);
        foreach (var line in code.Split(LineFeed))
        {
            text.Append(line.Length == 0 ? prefix.TrimEnd() : prefix + line).Append(LineFeed);
        }

        return text.Append(prefix).Append(marker).ToString();
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
