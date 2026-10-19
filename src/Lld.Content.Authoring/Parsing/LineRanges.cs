using System.Globalization;

namespace Lld.Content.Authoring.Parsing;

/// <summary>Line numbers written as ranges, e.g. <c>3-5,9</c> (1-based), as used by a code fence's <c>mark</c> attribute.</summary>
public static class LineRanges
{
    private const char Separator = ',';
    private const char Range = '-';

    /// <summary>The lines, sorted and without repeats, or <c>null</c> when the text isn't a valid list of ranges.</summary>
    public static IReadOnlyList<int>? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = new SortedSet<int>();
        foreach (var part in text.Split(Separator, StringSplitOptions.TrimEntries))
        {
            var bounds = part.Split(Range);
            if (bounds.Length > 2 || !TryLine(bounds[0], out var first))
            {
                return null;
            }

            var last = first;
            if (bounds.Length == 2 && (!TryLine(bounds[1], out last) || last < first))
            {
                return null;
            }

            for (var line = first; line <= last; line++)
            {
                lines.Add(line);
            }
        }

        return [.. lines];
    }

    /// <summary>Lines as the shortest ranges: 3, 4, 5, 9 → <c>3-5,9</c>.</summary>
    public static string Format(IEnumerable<int> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var ranges = new List<string>();
        int? start = null;
        var previous = 0;
        foreach (var line in lines.Distinct().Order())
        {
            if (start is not null && line == previous + 1)
            {
                previous = line;
                continue;
            }

            AddRange(ranges, start, previous);
            start = previous = line;
        }

        AddRange(ranges, start, previous);
        return string.Join(Separator, ranges);
    }

    private static void AddRange(List<string> ranges, int? start, int end)
    {
        if (start is { } first)
        {
            ranges.Add(first == end ? Text(first) : $"{Text(first)}{Range}{Text(end)}");
        }
    }

    private static string Text(int line) => line.ToString(CultureInfo.InvariantCulture);

    private static bool TryLine(string text, out int line) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out line) && line > 0;
}
