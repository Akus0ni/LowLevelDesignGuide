namespace Lld.Content.Authoring.Packing;

/// <summary>
/// Turns the changed lines of one side of a Before / After pair into the marks a reader sees. Marks help only when most
/// lines stay the same: when most of a side changed, the whole side is the change and marking every line is noise.
/// Blank lines between two marked lines are marked too, so a changed block reads as one band, not stripes.
/// </summary>
public static class LineMarks
{
    /// <summary>Above this share of changed (non-blank) lines, a side gets no marks.</summary>
    public const double MaxChangedShare = 0.6;

    private const char LineFeed = '\n';

    public static IReadOnlyList<int> ForDisplay(IReadOnlyList<int> changed, string code)
    {
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(code);
        var lines = code.Split(LineFeed);
        var nonBlank = lines.Count(line => line.Trim().Length > 0);
        if (changed.Count == 0 || changed.Count > MaxChangedShare * nonBlank)
        {
            return [];
        }

        var marked = changed.ToHashSet();
        return [.. Enumerable.Range(1, lines.Length).Where(line => marked.Contains(line) || IsBridge(line, lines, marked))];
    }

    /// <summary>A blank line whose nearest non-blank neighbours above and below are both marked.</summary>
    private static bool IsBridge(int line, string[] lines, HashSet<int> marked)
    {
        if (lines[line - 1].Trim().Length > 0)
        {
            return false;
        }

        var above = line - 1;
        while (above >= 1 && lines[above - 1].Trim().Length == 0)
        {
            above--;
        }

        var below = line + 1;
        while (below <= lines.Length && lines[below - 1].Trim().Length == 0)
        {
            below++;
        }

        return marked.Contains(above) && marked.Contains(below);
    }
}
