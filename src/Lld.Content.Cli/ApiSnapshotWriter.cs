using System.Text.Json;

namespace Lld.Content.Cli;

/// <summary>
/// Writes every <c>/api/content</c> response as a file, using the same catalog the dev API serves, so the Playwright
/// tests read real content without a running API. Path mapping (also used by <c>web/e2e</c>):
/// <code>
/// GET /api/content/modules                → modules.json
/// GET /api/content/modules/{slug}         → modules/{slug}.json
/// GET /api/content/lessons/{id}           → lessons/{id}.json
/// GET /api/content/cheatsheets            → cheatsheets.json
/// GET /api/content/cheatsheets?module={m} → cheatsheets-by-module/{m}.json
/// GET /api/content/cheatsheets/{lessonId} → cheatsheets/{lessonId}.json
/// </code>
/// JSON uses the ASP.NET Core web defaults (camelCase), exactly like the API.
/// </summary>
internal static class ApiSnapshotWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <returns>The number of files written.</returns>
    public static async Task<int> WriteAsync(IContentCatalog catalog, DirectoryInfo folder, CancellationToken cancellationToken)
    {
        if (folder.Exists)
        {
            folder.Delete(recursive: true); // A removed lesson must not leave a stale response behind.
        }

        folder.Create();
        var written = 0;
        async Task Write<T>(string relativePath, T value)
        {
            var path = Path.Combine(folder.FullName, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, value, Json, cancellationToken);
            written++;
        }

        var modules = await catalog.GetModulesAsync(cancellationToken);
        await Write("modules.json", modules);
        await Write("cheatsheets.json", await catalog.GetCheatSheetsAsync(null, cancellationToken));
        foreach (var module in modules)
        {
            var detail = await catalog.GetModuleAsync(module.Slug, cancellationToken);
            await Write($"modules/{module.Slug}.json", detail);
            await Write($"cheatsheets-by-module/{module.Slug}.json", await catalog.GetCheatSheetsAsync(module.Slug, cancellationToken));
            foreach (var lesson in detail!.Lessons)
            {
                await Write($"lessons/{lesson.Id}.json", await catalog.GetLessonAsync(lesson.Id, cancellationToken));
                await Write($"cheatsheets/{lesson.Id}.json", await catalog.GetCheatSheetAsync(lesson.Id, cancellationToken));
            }
        }

        return written;
    }
}
