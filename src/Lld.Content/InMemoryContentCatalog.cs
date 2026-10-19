using Lld.Contracts;

namespace Lld.Content;

/// <summary>
/// <see cref="IContentCatalog"/> over a <see cref="ContentPack"/> held in memory: used by the dev API and by
/// <c>lld-content snapshot</c>. Indexes are built once; every query is a dictionary lookup.
/// </summary>
public sealed class InMemoryContentCatalog : IContentCatalog
{
    private readonly IReadOnlyList<PackedModule> _modules;
    private readonly Dictionary<string, PackedModule> _modulesBySlug;
    private readonly Dictionary<string, (PackedModule Module, int Index)> _lessonsById;

    public InMemoryContentCatalog(ContentPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        _modules = [.. pack.Modules
            .OrderBy(m => m.Order)
            .Select(m => m with { Lessons = [.. m.Lessons.OrderBy(l => l.Order)] })];
        _modulesBySlug = _modules.ToDictionary(m => m.Slug, StringComparer.Ordinal);
        _lessonsById = _modules
            .SelectMany(m => m.Lessons.Select((lesson, index) => (lesson.Id, Entry: (m, index))))
            .ToDictionary(x => x.Id, x => x.Entry, StringComparer.Ordinal);
    }

    public Task<IReadOnlyList<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ModuleSummary>>(
            [.. _modules.Select(m => new ModuleSummary(m.Slug, m.Title, m.Summary, m.Order, m.Lessons.Count))]);

    public Task<ModuleDetail?> GetModuleAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(_modulesBySlug.TryGetValue(slug, out var module)
            ? new ModuleDetail(module.Slug, module.Title, module.Summary, [.. module.Lessons.Select(l => ToSummary(module, l))], module.Map, module.Mnemonics ?? [])
            : null);

    public Task<LessonDetail?> GetLessonAsync(string id, CancellationToken cancellationToken)
    {
        if (!_lessonsById.TryGetValue(id, out var entry))
        {
            return Task.FromResult<LessonDetail?>(null);
        }

        var (module, index) = entry;
        var lesson = module.Lessons[index];
        return Task.FromResult<LessonDetail?>(new LessonDetail(
            lesson.Id,
            module.Slug,
            module.Title,
            lesson.Slug,
            lesson.Title,
            lesson.Summary,
            lesson.Order,
            lesson.ReadingMinutes,
            lesson.Tags,
            lesson.BodyMarkdown,
            lesson.Outline,
            lesson.CheatSheet.Title,
            index > 0 ? ToLink(module, module.Lessons[index - 1]) : null,
            index < module.Lessons.Count - 1 ? ToLink(module, module.Lessons[index + 1]) : null,
            lesson.Terms,
            lesson.Icon));
    }

    public Task<IReadOnlyList<CheatSheetSummary>> GetCheatSheetsAsync(string? moduleSlug, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CheatSheetSummary>>(
            [.. _modules
                .Where(m => moduleSlug is null || m.Slug == moduleSlug)
                .SelectMany(m => m.Lessons.Select(l => new CheatSheetSummary(l.Id, m.Slug, m.Title, l.Slug, l.Title, l.CheatSheet.Title, l.Icon)))]);

    public Task<CheatSheetDetail?> GetCheatSheetAsync(string lessonId, CancellationToken cancellationToken)
    {
        if (!_lessonsById.TryGetValue(lessonId, out var entry))
        {
            return Task.FromResult<CheatSheetDetail?>(null);
        }

        var (module, index) = entry;
        var lesson = module.Lessons[index];
        return Task.FromResult<CheatSheetDetail?>(new CheatSheetDetail(
            lesson.Id, module.Slug, module.Title, lesson.Slug, lesson.Title, lesson.CheatSheet.Title, lesson.CheatSheet.BodyMarkdown, lesson.Icon));
    }

    private static LessonSummary ToSummary(PackedModule module, PackedLesson lesson) =>
        new(lesson.Id, module.Slug, lesson.Slug, lesson.Title, lesson.Summary, lesson.Order, lesson.ReadingMinutes, lesson.Tags, lesson.Icon, lesson.Hook,
            lesson.Outline.Count);

    private static LessonLink ToLink(PackedModule module, PackedLesson lesson) =>
        new(lesson.Id, module.Slug, lesson.Slug, lesson.Title);
}
