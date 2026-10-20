namespace Lld.Content.Authoring.Validation;

/// <summary>Key names used in <c>module.yml</c> and front-matter (validator and packer share them).</summary>
public static class FrontMatterKeys
{
    public const string Id = "id";
    public const string Title = "title";
    public const string Order = "order";
    public const string Summary = "summary";
    public const string Tags = "tags";
    public const string Source = "source";
    public const string Template = "template";
    public const string Map = "map";
    public const string Mnemonics = "mnemonics";
    public const string Icon = "icon";
    public const string Hook = "hook";
    public const string Symbol = "symbol";
    public const string Picker = "picker";
    public const string Lookalikes = "lookalikes";
}

/// <summary>The authoring contract (spec 003 §3.1). The module comes from the folder, so no file names it.</summary>
public static class ContentSchemas
{
    /// <summary>Where a case study comes from (the <c>lld-case-study</c> skill uses the same values).</summary>
    public static readonly IReadOnlyList<string> Sources = ["classic", "personal", "self-referential"];

    /// <summary>
    /// A module's lesson template (spec 004 §3.2). <c>concept</c> lessons need an interview-questions section and quote
    /// all their C# from compiled code.
    /// </summary>
    public const string ConceptTemplate = "concept";

    /// <summary>
    /// A design-pattern lesson (spec 006 §3.2): everything a concept lesson needs, plus fixed sections, a structure
    /// diagram, an Overuse stamp and a periodic-table <c>symbol</c>.
    /// </summary>
    public const string PatternTemplate = "pattern";

    /// <summary>
    /// A UML diagram lesson (spec 008 §3.2): everything a concept lesson needs, plus fixed sections (Notation, Read it,
    /// Code ↔ UML or From requirements, In an interview) and a Mistakes stamp.
    /// </summary>
    public const string DiagramTemplate = "diagram";

    public static readonly IReadOnlyList<string> Templates = [ConceptTemplate, PatternTemplate, DiagramTemplate];

    public static FrontMatterSchema Module { get; } = new(
        new FieldSpec(FrontMatterKeys.Title, FieldKind.Text, Required: true),
        new FieldSpec(FrontMatterKeys.Order, FieldKind.Order, Required: true),
        new FieldSpec(FrontMatterKeys.Summary, FieldKind.Text, Required: true),
        new FieldSpec(FrontMatterKeys.Template, FieldKind.Choice, Required: false, Templates),
        new FieldSpec(FrontMatterKeys.Map, FieldKind.Text, Required: false),
        new FieldSpec(FrontMatterKeys.Mnemonics, FieldKind.List, Required: false),
        new FieldSpec(FrontMatterKeys.Picker, FieldKind.Structured, Required: false),
        new FieldSpec(FrontMatterKeys.Lookalikes, FieldKind.Structured, Required: false));

    public static FrontMatterSchema Lesson { get; } = new(
        new FieldSpec(FrontMatterKeys.Id, FieldKind.Text, Required: true),
        new FieldSpec(FrontMatterKeys.Title, FieldKind.Text, Required: true),
        new FieldSpec(FrontMatterKeys.Order, FieldKind.Order, Required: true),
        new FieldSpec(FrontMatterKeys.Summary, FieldKind.Text, Required: true),
        new FieldSpec(FrontMatterKeys.Tags, FieldKind.List, Required: true),
        new FieldSpec(FrontMatterKeys.Source, FieldKind.Choice, Required: false, Sources),
        new FieldSpec(FrontMatterKeys.Icon, FieldKind.Text, Required: false),
        new FieldSpec(FrontMatterKeys.Hook, FieldKind.Text, Required: false),
        new FieldSpec(FrontMatterKeys.Symbol, FieldKind.Text, Required: false));

    public static FrontMatterSchema CheatSheet { get; } = new(
        new FieldSpec(FrontMatterKeys.Title, FieldKind.Text, Required: true));
}
