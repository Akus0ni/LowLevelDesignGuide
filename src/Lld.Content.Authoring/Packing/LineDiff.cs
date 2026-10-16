namespace Lld.Content.Authoring.Packing;

/// <summary>Which lines of a Before / After pair changed (1-based).</summary>
/// <param name="Removed">Lines of <i>Before</i> that are not in <i>After</i>.</param>
/// <param name="Added">Lines of <i>After</i> that are new or changed.</param>
public sealed record LineChanges(IReadOnlyList<int> Removed, IReadOnlyList<int> Added);

/// <summary>
/// A line diff by longest common subsequence. Snippets are a few dozen lines, so the simple O(n·m) table is fast enough.
/// Trailing whitespace and blank lines never count as changes.
/// </summary>
public static class LineDiff
{
    private const char LineFeed = '\n';

    public static LineChanges Compare(string before, string after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var a = Lines(before);
        var b = Lines(after);
        var common = CommonLengths(a, b);
        var removed = new List<int>();
        var added = new List<int>();
        int i = 0, j = 0;
        while (i < a.Length || j < b.Length)
        {
            if (i < a.Length && j < b.Length && a[i] == b[j])
            {
                i++;
                j++;
            }
            else if (j < b.Length && (i == a.Length || common[i, j + 1] >= common[i + 1, j]))
            {
                AddIfNotBlank(added, b, j++);
            }
            else
            {
                AddIfNotBlank(removed, a, i++);
            }
        }

        return new LineChanges(removed, added);
    }

    private static string[] Lines(string code) => [.. code.Split(LineFeed).Select(line => line.TrimEnd())];

    /// <summary><c>table[i, j]</c> = length of the longest common subsequence of <c>a[i..]</c> and <c>b[j..]</c>.</summary>
    private static int[,] CommonLengths(string[] a, string[] b)
    {
        var table = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
        {
            for (var j = b.Length - 1; j >= 0; j--)
            {
                table[i, j] = a[i] == b[j] ? table[i + 1, j + 1] + 1 : Math.Max(table[i + 1, j], table[i, j + 1]);
            }
        }

        return table;
    }

    private static void AddIfNotBlank(List<int> lines, string[] text, int index)
    {
        if (text[index].Length > 0)
        {
            lines.Add(index + 1);
        }
    }
}
