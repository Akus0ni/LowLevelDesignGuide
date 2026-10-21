using System.Text;
using Lld.Contracts;

namespace Lld.Search.Query;

/// <summary>
/// Reads text that FTS5's <c>highlight()</c> or <c>snippet()</c> marked with <see cref="Open"/> / <see cref="Close"/>
/// (private-use characters, never in content). Marks become ranges, so no HTML crosses the bridge (plan §2.5).
/// </summary>
public static class Highlighted
{
    public const string Open = "\uE000";
    public const string Close = "\uE001";

    /// <summary>A snippet shows at most this many marks (spec Q3).</summary>
    public const int MaxSnippetMarks = 3;

    /// <summary>
    /// Markdown emphasis and code markers a note body may hold (content text is already plain). They are dropped before
    /// the ranges are read, so the marks still line up with the text shown.
    /// </summary>
    private static readonly string[] MarkdownMarkers = ["**", "__", "`"];

    public static SearchSnippetDto Snippet(string marked)
    {
        ArgumentNullException.ThrowIfNull(marked);
        marked = MarkdownMarkers.Aggregate(marked, (text, marker) => text.Replace(marker, string.Empty, StringComparison.Ordinal));
        var text = new StringBuilder(marked.Length);
        var marks = new List<int[]>();
        var start = -1;
        foreach (var c in marked)
        {
            if (c == Open[0])
            {
                start = text.Length;
            }
            else if (c == Close[0])
            {
                if (start >= 0 && text.Length > start && marks.Count < MaxSnippetMarks)
                {
                    marks.Add([start, text.Length - start]);
                }

                start = -1;
            }
            else
            {
                text.Append(c);
            }
        }

        return new SearchSnippetDto(text.ToString(), marks);
    }

    /// <summary>The marked words as they appear in the text (<c>Inheritance</c>, <c>inherits</c>), for the reader's marks.</summary>
    public static IEnumerable<string> Words(string marked)
    {
        ArgumentNullException.ThrowIfNull(marked);
        var from = 0;
        while ((from = marked.IndexOf(Open, from, StringComparison.Ordinal)) >= 0)
        {
            var end = marked.IndexOf(Close, from, StringComparison.Ordinal);
            if (end < 0)
            {
                yield break;
            }

            var word = marked[(from + 1)..end];
            if (word.Length > 0)
            {
                yield return word;
            }

            from = end + 1;
        }
    }
}
