namespace Lld.Content.Authoring.Snippets;

/// <summary>Removes the indentation every non-blank line shares, so quoted code starts at column 0.</summary>
public static class Dedent
{
    private const string LineFeed = "\n";

    public static string Apply(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var lines = code.ReplaceLineEndings(LineFeed).Split(LineFeed).Select(line => line.TrimEnd()).ToList();
        var indent = lines.Where(line => line.Length > 0).Select(IndentOf).DefaultIfEmpty(0).Min();
        return string.Join(LineFeed, lines.Select(line => line.Length == 0 ? line : line[indent..])).Trim('\n');
    }

    private static int IndentOf(string line) => line.Length - line.TrimStart().Length;
}
