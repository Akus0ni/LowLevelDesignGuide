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

    public const string FenceSyntax = "fence.syntax";
    public const string FenceUnknownFlag = "fence.unknown_flag";
    public const string FenceUnknownAttribute = "fence.unknown_attribute";
    public const string FenceBadMark = "fence.bad_mark";
    public const string FenceMarkOnCompare = "fence.mark_on_compare";
    public const string FenceDiagramKeyNotMermaid = "fence.diagram_key_not_mermaid";
    public const string FenceStepsNotFlowchart = "fence.steps_not_flowchart";

    public const string SnippetHasCode = "snippet.has_code";
    public const string SnippetNotCSharp = "snippet.not_csharp";
    public const string SnippetBadReference = "snippet.bad_reference";
    public const string SnippetFileMissing = "snippet.file_missing";
    public const string SnippetSymbolMissing = "snippet.symbol_missing";
    public const string SnippetSymbolAmbiguous = "snippet.symbol_ambiguous";

    public const string CompareUnpaired = "compare.unpaired";
    public const string CompareLanguageMismatch = "compare.language_mismatch";

    public const string CalloutUnknownType = "callout.unknown_type";

    public const string InterviewMissing = "interview.missing";
    public const string InterviewCount = "interview.count";
    public const string InterviewNoAnswer = "interview.no_answer";
    public const string CodeInlineCSharp = "code.inline_csharp";

    public const string MapNotFlowchart = "map.not_flowchart";
    public const string MapUnknownNode = "map.unknown_node";
    public const string MapLessonMissing = "map.lesson_missing";

    public const string RememberMissing = "remember.missing";
    public const string ConceptIconMissing = "concept.icon";
    public const string ConceptHookMissing = "concept.hook_missing";
    public const string ConceptHookTooLong = "concept.hook_too_long";
    public const string MnemonicFormat = "mnemonic.format";
    public const string MnemonicUnknownLesson = "mnemonic.unknown_lesson";
    public const string MnemonicLetterMismatch = "mnemonic.letter_mismatch";

    public const string PatternSectionMissing = "pattern.section_missing";
    public const string PatternSectionOrder = "pattern.section_order";
    public const string PatternStructureNoClassDiagram = "pattern.structure_no_class_diagram";
    public const string PatternSeeItNoCompare = "pattern.see_it_no_compare";
    public const string OveruseMissing = "overuse.missing";
    public const string OveruseDuplicate = "overuse.duplicate";
    public const string DiagramSectionMissing = "diagram.section_missing";
    public const string DiagramSectionNoDiagram = "diagram.section_no_diagram";
    public const string MistakesMissing = "mistakes.missing";
    public const string MistakesDuplicate = "mistakes.duplicate";
    public const string CodePairUnresolved = "code_pair.unresolved";
    public const string CodePairNotSupported = "code_pair.not_supported";
    public const string CodePairNoTypes = "code_pair.no_types";
    public const string CodePairMismatch = "code_pair.mismatch";
    public const string SymbolMissing = "symbol.missing";
    public const string SymbolFormat = "symbol.format";
    public const string SymbolDuplicate = "symbol.duplicate";

    public const string PickerShape = "picker.shape";
    public const string PickerUnknownNext = "picker.unknown_next";
    public const string PickerCycle = "picker.cycle";
    public const string PickerTooDeep = "picker.too_deep";
    public const string PickerUnknownLesson = "picker.unknown_lesson";
    public const string PickerUnreachable = "picker.unreachable";
    public const string LookalikeShape = "lookalike.shape";
    public const string LookalikeUnknownLesson = "lookalike.unknown_lesson";
    public const string LookalikeDuplicate = "lookalike.duplicate";
}
