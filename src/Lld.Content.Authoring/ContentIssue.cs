namespace Lld.Content.Authoring;

/// <summary>A problem in <c>content/</c>, reported as <c>path:line: code: message</c> (the format editors and CI understand).</summary>
/// <param name="Path">Repository-relative path with forward slashes.</param>
/// <param name="Line">1-based line, when the problem is on a specific line.</param>
/// <param name="Code">Stable identifier, e.g. <c>cheatsheet.missing</c>; tests and docs refer to it.</param>
public sealed record ContentIssue(string Path, int? Line, string Code, string Message)
{
    public override string ToString() =>
        Line is { } line ? $"{Path}:{line}: {Code}: {Message}" : $"{Path}: {Code}: {Message}";
}
