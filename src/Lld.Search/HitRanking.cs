using Lld.Contracts;

namespace Lld.Search;

/// <summary>A unit that matched, before snippets are made (phase 1, plan §2.5).</summary>
/// <param name="Rank">bm25: lower is better.</param>
/// <param name="TitleMatch">A query word matched the lesson title.</param>
/// <param name="BodyMatch">A query word matched the section heading or text (or the note).</param>
/// <param name="WholeTitleMatch">Every query word matched the lesson title.</param>
public sealed record SearchCandidate(
    long Id,
    string Kind,
    string ModuleSlug,
    int ModuleOrder,
    string LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonOrder,
    int SectionOrder,
    string Anchor,
    string Section,
    string? NoteId,
    double Rank,
    bool TitleMatch,
    bool BodyMatch,
    bool WholeTitleMatch = false);

/// <summary>Candidates in result order, collapsed, with the counts the filter chips show.</summary>
public sealed record RankedHits(IReadOnlyList<SearchCandidate> Hits, IReadOnlyDictionary<string, int> CountsByKind, int Total);

/// <summary>
/// Orders and trims search candidates (spec Q1, Q2):
/// <list type="number">
/// <item>Every unit of a lesson carries the lesson title, so a title-only match would list each section. Those are dropped
/// when another unit of the lesson (same kind) matched, otherwise they collapse into the lesson's intro.</item>
/// <item>Order: a match of every word in the lesson title, then of some word, the lesson's intro leading; then bm25, then course order (module, lesson,
/// kind, section).</item>
/// <item>The kind filter applies after counting, so the chips show what each kind would give.</item>
/// </list>
/// </summary>
public static class HitRanking
{
    public static RankedHits Rank(IEnumerable<SearchCandidate> candidates, string? kind, int limit)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var all = candidates.ToList();
        var bodyMatches = all.Where(c => c.BodyMatch).ToList();
        var matchedLessons = bodyMatches.Select(c => (c.Kind, c.LessonId)).ToHashSet();
        var kept = bodyMatches.Concat(TitleOnlyPerLesson(all.Where(c => !c.BodyMatch && !matchedLessons.Contains((c.Kind, c.LessonId)))))
            .OrderByDescending(c => c.WholeTitleMatch)
            .ThenByDescending(c => c.TitleMatch)
            .ThenByDescending(c => c.TitleMatch && IsLessonIntro(c))
            .ThenBy(c => c.Rank)
            .ThenBy(c => c.ModuleOrder)
            .ThenBy(c => c.LessonOrder)
            .ThenBy(c => KindIndex(c.Kind))
            .ThenBy(c => c.SectionOrder)
            .ThenBy(c => c.Id)
            .ToList();
        var counts = SearchKinds.All.ToDictionary(k => k, k => kept.Count(c => c.Kind == k), StringComparer.Ordinal);
        var filtered = kind is null ? kept : kept.Where(c => c.Kind == kind).ToList();
        return new RankedHits([.. filtered.Take(Math.Max(0, limit))], counts, filtered.Count);
    }

    /// <summary>The lesson's own entry (its intro): the Best match when the title matched (spec Q2).</summary>
    private static bool IsLessonIntro(SearchCandidate c) => c.Kind == SearchKinds.Lesson && c.Anchor.Length == 0;

    /// <summary>
    /// For a lesson and kind where nothing matched but the title: one hit, the first section (the intro), ranked as the
    /// group's best. Where something else of that lesson matched, title-only units add nothing and are dropped.
    /// </summary>
    private static IEnumerable<SearchCandidate> TitleOnlyPerLesson(IEnumerable<SearchCandidate> titleOnly) =>
        titleOnly
            .GroupBy(c => (c.Kind, c.LessonId))
            .Select(group => group.OrderBy(c => c.SectionOrder).ThenBy(c => c.Id).First() with { Rank = group.Min(c => c.Rank) });

    private static int KindIndex(string kind) => kind switch
    {
        SearchKinds.Lesson => 0,
        SearchKinds.CheatSheet => 1,
        SearchKinds.Note => 2,
        _ => 3,
    };
}
