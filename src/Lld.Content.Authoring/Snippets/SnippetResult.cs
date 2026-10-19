namespace Lld.Content.Authoring.Snippets;

/// <summary>The code a <see cref="SnippetReference"/> quotes, or why there is none.</summary>
public abstract record SnippetResult
{
    private SnippetResult()
    {
    }

    /// <summary>De-indented code with <c>\n</c> line endings.</summary>
    public sealed record Found(string Code) : SnippetResult;

    public sealed record FileMissing(string Path) : SnippetResult;

    public sealed record SymbolMissing(string Selector) : SnippetResult;

    /// <summary>The selector names more than one declaration (e.g. two nested types called <c>Before</c>).</summary>
    public sealed record Ambiguous(string Selector, int Count) : SnippetResult;
}

/// <summary>Finds the code a snippet fence quotes.</summary>
public interface ICodeSnippetSource
{
    SnippetResult Extract(SnippetReference reference);

    /// <summary>Every file read so far, so the dev API can reload when one changes.</summary>
    IReadOnlySet<string> FilesRead { get; }
}
