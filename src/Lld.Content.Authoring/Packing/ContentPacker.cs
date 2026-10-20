using System.Security.Cryptography;
using Lld.Content.Authoring.Editing;
using Lld.Content.Authoring.Links;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;
using Lld.Content.Authoring.Snippets;
using Lld.Content.Authoring.Validation;
using Lld.Contracts;

namespace Lld.Content.Authoring.Packing;

/// <summary>
/// Turns validated content into a <see cref="ContentPack"/>: ordered, links rewritten, outlines and reading times
/// computed. The version is a hash of everything else, so the same content always gets the same version and the
/// device reloads only when something really changed. Call it only after validation passed.
/// </summary>
public sealed class ContentPacker(ContentOptions options, ICodeSnippetSource snippets)
{
    private readonly IReadOnlyList<IBodyEditSource> _editors =
        [new LinkRewriter(new LinkResolver(options), options), new FenceEdits(snippets)];

    public ContentPack Pack(ContentSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var modules = set.Modules
            .Select(PackModule)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Slug, StringComparer.Ordinal)
            .ToList();
        var unversioned = new ContentPack(ContentPack.CurrentFormat, string.Empty, modules);
        return unversioned with { Version = VersionOf(unversioned) };
    }

    private PackedModule PackModule(SourceModule module)
    {
        var manifest = module.Manifest ?? throw NotValidated(module.ManifestPath);
        var lessons = module.Lessons
            .Select(PackLesson)
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Slug, StringComparer.Ordinal)
            .ToList();
        return new PackedModule(
            module.Slug, Text(manifest, FrontMatterKeys.Title), Text(manifest, FrontMatterKeys.Summary), Order(manifest), lessons,
            manifest[FrontMatterKeys.Map]?.Scalar is { } map ? FlowchartMap.Parse(map).ToContract() : null,
            [.. (manifest[FrontMatterKeys.Mnemonics]?.Items ?? []).Select(Mnemonics.Parse).Select(m => new Mnemonic(m.Word, m.Lessons))],
            manifest[FrontMatterKeys.Picker]?.Nested is { } picker ? PickerTree.Parse(picker).ToContract() : null,
            manifest[FrontMatterKeys.Lookalikes]?.Nested is { } lookalikes ? [.. Lookalikes.Parse(lookalikes).Entries.Select(l => l.ToContract())] : []);
    }

    private PackedLesson PackLesson(SourceLesson lesson)
    {
        var body = lesson.Lesson ?? throw NotValidated(lesson.LessonPath);
        var sheet = lesson.CheatSheet ?? throw NotValidated(lesson.CheatSheetPath);
        var front = body.FrontMatter ?? throw NotValidated(body.Path);
        var sheetFront = sheet.FrontMatter ?? throw NotValidated(sheet.Path);
        var headings = DocumentHeadings.Of(body);
        return new PackedLesson(
            Text(front, FrontMatterKeys.Id),
            lesson.Slug,
            Text(front, FrontMatterKeys.Title),
            Text(front, FrontMatterKeys.Summary),
            Order(front),
            front[FrontMatterKeys.Tags]?.Items ?? [],
            front[FrontMatterKeys.Source]?.Scalar,
            ReadingTime.Minutes(body.Document, options.WordsPerMinute),
            Body(body),
            DocumentHeadings.Outline(headings),
            new PackedCheatSheet(Text(sheetFront, FrontMatterKeys.Title), Body(sheet)),
            EntityTerms.Of(body, headings),
            front[FrontMatterKeys.Icon]?.Scalar?.Trim(),
            front[FrontMatterKeys.Hook]?.Scalar?.Trim(),
            front[FrontMatterKeys.Symbol]?.Scalar?.Trim());
    }

    private string Body(SourceDocument document) =>
        BodyComposer.Compose(document, _editors.SelectMany(editor => editor.EditsFor(document)));

    private static string VersionOf(ContentPack unversioned) =>
        Convert.ToHexStringLower(SHA256.HashData(ContentPackJson.ToUtf8Bytes(unversioned)));

    private static string Text(YamlMap map, string key) => map[key]?.Scalar?.Trim() ?? string.Empty;

    private static int Order(YamlMap map) =>
        FrontMatterSchema.TryParseOrder(map[FrontMatterKeys.Order]?.Scalar, out var order) ? order : 0;

    private static InvalidOperationException NotValidated(string path) =>
        new($"{path} is missing or has no front-matter: validate content before packing it.");
}
