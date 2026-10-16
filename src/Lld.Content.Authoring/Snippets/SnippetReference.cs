using System.Text.RegularExpressions;

namespace Lld.Content.Authoring.Snippets;

/// <summary>
/// What a snippet fence quotes (spec 004 §3.3): <c>samples/X/Y.cs#Type,Type.Member</c>. The path is relative to the
/// repository root; no selector means the whole file.
/// </summary>
public sealed partial record SnippetReference(string Path, IReadOnlyList<string> Selectors)
{
    public const string Example = "samples/Lld.Samples.OopSolid/Srp/After/Report.cs#Report";

    private const char SelectorStart = '#';
    private const string CSharpExtension = ".cs";

    public override string ToString() =>
        Selectors.Count == 0 ? Path : $"{Path}{SelectorStart}{string.Join(',', Selectors)}";

    /// <summary>The reference, or why the text isn't one.</summary>
    public static (SnippetReference? Reference, string? Problem) Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var hash = value.IndexOf(SelectorStart, StringComparison.Ordinal);
        var path = hash < 0 ? value : value[..hash];
        if (!IsRepositoryPath(path))
        {
            return (null, $"'{value}' must start with a repository path to a .cs file, e.g. {Example}.");
        }

        if (hash < 0)
        {
            return (new SnippetReference(path, []), null);
        }

        var selectors = SplitSelectors(value[(hash + 1)..]);
        var bad = selectors.FirstOrDefault(s => !SelectorPattern().IsMatch(s));
        return bad is null && selectors.Count > 0
            ? (new SnippetReference(path, selectors), null)
            : (null, $"'{value}': '{bad ?? string.Empty}' isn't a type or member name (Type, Outer.Inner, Type.Member, Box<T>).");
    }

    private static bool IsRepositoryPath(string path) =>
        path.EndsWith(CSharpExtension, StringComparison.Ordinal)
        && !path.StartsWith('/')
        && !path.Contains('\\', StringComparison.Ordinal)
        && path.Split('/').All(segment => segment is not ("" or "." or ".."));

    /// <summary>Splits on commas outside generic brackets: <c>A,Pair&lt;K, V&gt;</c> → A, Pair&lt;K, V&gt;.</summary>
    private static List<string> SplitSelectors(string text)
    {
        var selectors = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            var c = i < text.Length ? text[i] : ',';
            depth += c switch { '<' => 1, '>' => -1, _ => 0 };
            if (c == ',' && depth == 0)
            {
                selectors.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        return selectors;
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*(<\s*\w+(\s*,\s*\w+)*\s*>)?(\.[A-Za-z_]\w*(<\s*\w+(\s*,\s*\w+)*\s*>)?)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SelectorPattern();
}
