namespace Lld.Content.Authoring.Validation;

/// <summary>Stable codes for every validation problem; tests and the authoring guide refer to them.</summary>
public static class IssueCodes
{
    public const string ModuleManifestMissing = "module.manifest_missing";
    public const string LessonMissing = "lesson.missing";
    public const string CheatSheetMissing = "cheatsheet.missing";

    public const string FrontMatterMissing = "frontmatter.missing";
    public const string FrontMatterMissingKey = "frontmatter.missing_key";
    public const string FrontMatterWrongType = "frontmatter.wrong_type";
    public const string FrontMatterUnknownKey = "frontmatter.unknown_key";

    public const string IdDuplicate = "id.duplicate";
    public const string IdFormat = "id.format";
    public const string SlugFormat = "slug.format";
    public const string OrderDuplicate = "order.duplicate";

    public const string LinkInvalid = "link.invalid";
    public const string LinkBroken = "link.broken";
    public const string LinkAnchorMissing = "link.anchor_missing";
    public const string LinkRepositoryFileMissing = "link.repo_file_missing";

    public const string MermaidEmpty = "mermaid.empty";
    public const string ImageUnsupported = "image.unsupported";
}
