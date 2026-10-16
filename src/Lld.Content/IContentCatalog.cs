using Lld.Contracts;

namespace Lld.Content;

/// <summary>
/// Read-only access to modules, lessons and cheat sheets (Repository port). Implemented in memory for the dev API
/// and over SQLite on the device; both pass the same contract tests. Missing items return <c>null</c>.
/// </summary>
public interface IContentCatalog
{
    /// <summary>All modules in display order.</summary>
    Task<IReadOnlyList<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken);

    Task<ModuleDetail?> GetModuleAsync(string slug, CancellationToken cancellationToken);

    Task<LessonDetail?> GetLessonAsync(string id, CancellationToken cancellationToken);

    /// <summary>Every cheat sheet in module and lesson order; only one module's when <paramref name="moduleSlug"/> is set.</summary>
    Task<IReadOnlyList<CheatSheetSummary>> GetCheatSheetsAsync(string? moduleSlug, CancellationToken cancellationToken);

    Task<CheatSheetDetail?> GetCheatSheetAsync(string lessonId, CancellationToken cancellationToken);
}
