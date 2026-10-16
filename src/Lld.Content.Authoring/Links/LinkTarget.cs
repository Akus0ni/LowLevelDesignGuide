namespace Lld.Content.Authoring.Links;

/// <summary>What a link in a content file points at, once resolved against the file's own folder.</summary>
public abstract record LinkTarget
{
    private LinkTarget()
    {
    }

    /// <summary><c>https://…</c>, <c>mailto:…</c>: left as written.</summary>
    public sealed record External(string Url) : LinkTarget;

    /// <summary><c>#heading</c> in the same file.</summary>
    public sealed record SameDocument(string Anchor) : LinkTarget;

    /// <summary>Another lesson's <c>lesson.md</c>, optionally at a heading.</summary>
    public sealed record Lesson(string ModuleSlug, string LessonSlug, string? Anchor) : LinkTarget
    {
        public string Path(string contentDirectory) => $"{contentDirectory}/{ModuleSlug}/{LessonSlug}/lesson.md";
    }

    /// <summary>A lesson's <c>cheatsheet.md</c>, optionally at a heading.</summary>
    public sealed record CheatSheet(string ModuleSlug, string LessonSlug, string? Anchor) : LinkTarget
    {
        public string Path(string contentDirectory) => $"{contentDirectory}/{ModuleSlug}/{LessonSlug}/cheatsheet.md";
    }

    /// <summary>Any other repository file (usually source code), opened on GitHub.</summary>
    /// <param name="Path">Repository-relative path.</param>
    /// <param name="Fragment">Kept as written, e.g. <c>L10-L20</c>.</param>
    public sealed record RepositoryFile(string Path, string? Fragment) : LinkTarget;

    /// <summary>A link that can't work (outside the repository, absolute path, ...); <see cref="Reason"/> says why.</summary>
    public sealed record Invalid(string Reason) : LinkTarget;
}
