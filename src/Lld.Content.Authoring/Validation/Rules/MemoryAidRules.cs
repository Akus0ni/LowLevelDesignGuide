using System.Globalization;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// Concept lessons and their cheat sheets open with a REMEMBER stamp: a <c>[!REMEMBER]</c> callout before the first
/// <c>##</c> section (spec 004 §3.8).
/// </summary>
public sealed class RememberStampRule : IContentRule
{
    private const int FirstLine = 1;
    private const int SectionLevel = 2;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from lesson in context.ConceptLessons
        from document in new[] { lesson.Lesson, lesson.CheatSheet }.OfType<SourceDocument>()
        where !OpensWithStamp(document)
        select new ContentIssue(document.Path, FirstLine, IssueCodes.RememberMissing,
            "Open with a stamp: '> [!REMEMBER]' and one or two short lines, before the first '##' section.");

    private static bool OpensWithStamp(SourceDocument document)
    {
        var firstSection = DocumentHeadings.Of(document).FirstOrDefault(h => h.Level == SectionLevel)?.Line ?? int.MaxValue;
        return Callouts.Of(document).Any(c => c.Type == Callouts.Remember && c.Line < firstSection);
    }
}

/// <summary>Every concept lesson has a face: an <c>icon</c> (one emoji) and a <c>hook</c> of at most 40 characters.</summary>
public sealed class ConceptIdentityRule : IContentRule
{
    public const int MaxHookLength = 40;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.ConceptLessons
            .Select(l => l.Lesson)
            .OfType<SourceDocument>()
            .Where(d => d.FrontMatter is not null)
            .SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var front = lesson.FrontMatter!;
        var icon = front[FrontMatterKeys.Icon];
        if (icon?.Scalar is not { Length: > 0 } iconText)
        {
            yield return new(lesson.Path, front.Line, IssueCodes.ConceptIconMissing, "Add 'icon:' with one emoji, e.g. icon: \"🔒\".");
        }
        else if (new StringInfo(iconText.Trim()).LengthInTextElements != 1)
        {
            yield return new(lesson.Path, icon.Line, IssueCodes.ConceptIconMissing, $"'icon' must be one emoji, not '{iconText}'.");
        }

        var hook = front[FrontMatterKeys.Hook];
        if (hook?.Scalar is not { Length: > 0 } hookText)
        {
            yield return new(lesson.Path, front.Line, IssueCodes.ConceptHookMissing, "Add 'hook:', a phrase to remember, e.g. hook: \"Ask, don't touch\".");
        }
        else if (hookText.Trim().Length > MaxHookLength)
        {
            yield return new(lesson.Path, hook.Line, IssueCodes.ConceptHookTooLong,
                $"'hook' is {hookText.Trim().Length} characters; keep it to {MaxHookLength} so it fits on a chip.");
        }
    }
}

/// <summary>
/// Each mnemonic in <c>module.yml</c> reads <c>"WORD: lesson, lesson, ..."</c>, names lessons of the module, and each
/// letter is the first letter of its lesson's title (spec 004 §3.8).
/// </summary>
public sealed class MnemonicRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules.SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceModule module)
    {
        if (module.Manifest?[FrontMatterKeys.Mnemonics] is not { } entry)
        {
            yield break;
        }

        var titles = module.Lessons
            .Where(l => l.Lesson?.FrontMatter?[FrontMatterKeys.Title]?.Scalar is not null)
            .ToDictionary(l => l.Slug, l => l.Lesson!.FrontMatter![FrontMatterKeys.Title]!.Scalar!.Trim(), StringComparer.Ordinal);
        foreach (var mnemonic in entry.Items.Select(Mnemonics.Parse))
        {
            if (mnemonic.Problem is not null)
            {
                yield return new(module.ManifestPath, entry.Line, IssueCodes.MnemonicFormat, mnemonic.Problem);
                continue;
            }

            var letters = Mnemonics.Letters(mnemonic.Word);
            for (var i = 0; i < letters.Count; i++)
            {
                var slug = mnemonic.Lessons[i];
                if (!titles.TryGetValue(slug, out var title))
                {
                    yield return new(module.ManifestPath, entry.Line, IssueCodes.MnemonicUnknownLesson,
                        $"'{mnemonic.Word}': '{slug}' isn't a lesson in this module.");
                }
                else if (char.ToUpperInvariant(title[0]) != char.ToUpperInvariant(letters[i]))
                {
                    yield return new(module.ManifestPath, entry.Line, IssueCodes.MnemonicLetterMismatch,
                        $"'{mnemonic.Word}': letter '{letters[i]}' should start the title of '{slug}' ('{title}').");
                }
            }
        }
    }
}
