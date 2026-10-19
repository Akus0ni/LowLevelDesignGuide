using Lld.Content.Authoring.Validation.Rules;

namespace Lld.Content.Authoring.Validation;

/// <summary>Runs every rule and returns all issues, sorted by file and line so output is stable.</summary>
public sealed class ContentValidator(IEnumerable<IContentRule> rules)
{
    public static IReadOnlyList<IContentRule> DefaultRules { get; } =
    [
        new ModuleManifestRequiredRule(),
        new LessonRequiredRule(),
        new CheatSheetRequiredRule(),
        new ModuleManifestSchemaRule(),
        new LessonFrontMatterRule(),
        new CheatSheetFrontMatterRule(),
        new LessonIdUniqueRule(),
        new IdentifierFormatRule(),
        new OrderUniqueRule(),
        new InternalLinksRule(),
        new RepositoryLinksRule(),
        new ImagesNotSupportedRule(),
        new MermaidNotEmptyRule(),
        new FenceInfoRule(),
        new SnippetFenceRule(),
        new SnippetResolvesRule(),
        new ComparePairRule(),
        new CalloutTypeRule(),
        new InterviewQuestionsRule(),
        new ConceptCodeFromSnippetsRule(),
        new ModuleMapRule(),
        new RememberStampRule(),
        new ConceptIdentityRule(),
        new MnemonicRule(),
        new PatternSectionsRule(),
        new PatternStructureRule(),
        new PatternCompareRule(),
        new OveruseStampRule(),
        new SymbolRule(),
        new PickerRule(),
        new LookalikesRule(),
    ];

    public ContentValidator()
        : this(DefaultRules)
    {
    }

    public IReadOnlyList<ContentIssue> Validate(ContentContext context) =>
        [.. rules.SelectMany(rule => rule.Check(context))
            .OrderBy(i => i.Path, StringComparer.Ordinal)
            .ThenBy(i => i.Line ?? 0)
            .ThenBy(i => i.Code, StringComparer.Ordinal)];
}
