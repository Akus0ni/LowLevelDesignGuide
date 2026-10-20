using Lld.Contracts;

namespace Lld.Search;

/// <summary>A unit that matched, before snippets are made (phase 1, plan §2.5).</summary>
/// <param name="Rank">bm25: lower is better.</param>
/// <param name="TitleMatch">A query word matched the lesson title.</param>
/// <param name="BodyMatch">A query word matched the section heading or text (or the note).</param>
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
    bool BodyMatch);

/// <summary>Candidates in result order, collapsed, with the counts the filter chips show.</summary>
public sealed record RankedHits(IReadOnlyList<SearchCandidate> Hits, IReadOnlyDictionary<string, int> CountsByKind, int Total);

/// <summary>
/// Orders and trims search candidates (spec Q1, Q2):
/// <list type="number">
/// <item>Every unit of a lesson carries the lesson title, so a title-only match would list each section. Those collapse
/// into one hit per lesson and kind (the best one).</item>
/// <item>Order: a lesson-title match first, then bm25, then course order (module, lesson, kind, section).</item>
/// <item>The kind filter applies after counting, so the chips show what each kind would give.</item>
/// </list>
/// </summary>
public static class HitRanking
{
    public static RankedHits Rank(IEnumerable<SearchCandidate> candidates, string? kind, int limit)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var all = candidates.ToList();
        var kept = all.Where(c => c.BodyMatch).Concat(TitleOnlyPerLesson(all.Where(c => !c.BodyMatch)))
            .OrderByDescending(c => c.TitleMatch)
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

    /// <summary>
    /// One hit per lesson and kind for units that matched on the lesson title alone: the first section (a lesson's intro),
    /// ranked as the group's best.
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
