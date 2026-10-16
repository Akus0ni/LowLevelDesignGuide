using Lld.Content;
using Lld.Contracts;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data.Content;

/// <summary>
/// <see cref="IContentCatalog"/> over the device database. Hand-written queries, each mapped by a small row reader;
/// previous / next come from SQLite window functions over the module's lessons. Passes the same contract tests as
/// the in-memory catalog the dev API uses.
/// </summary>
public sealed class SqliteContentCatalog(DeviceDatabase database) : IContentCatalog
{
    private const string LessonOrder = "l.SortOrder, l.Slug";
    private const string ModuleOrder = "m.SortOrder, m.Slug";

    public Task<IReadOnlyList<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken) =>
        QueryAsync(
            $"""
            SELECT m.Slug, m.Title, m.Summary, m.SortOrder, COUNT(l.Id)
            FROM Modules m LEFT JOIN Lessons l ON l.ModuleSlug = m.Slug
            GROUP BY m.Slug
            ORDER BY {ModuleOrder};
            """,
            [],
            r => new ModuleSummary(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetInt32(4)),
            cancellationToken);

    public async Task<ModuleDetail?> GetModuleAsync(string slug, CancellationToken cancellationToken)
    {
        var module = (await QueryAsync(
            "SELECT Slug, Title, Summary, Map FROM Modules WHERE Slug = $slug;",
            [("$slug", slug)],
            r => (Slug: r.GetString(0), Title: r.GetString(1), Summary: r.GetString(2), Map: r.IsDBNull(3) ? null : r.GetString(3)),
            cancellationToken).ConfigureAwait(false)).SingleOrDefault();
        if (module.Slug is null)
        {
            return null;
        }

        var lessons = await QueryAsync(
            $"""
            SELECT l.Id, l.ModuleSlug, l.Slug, l.Title, l.Summary, l.SortOrder, l.ReadingMinutes, l.TagsJson
            FROM Lessons l WHERE l.ModuleSlug = $slug
            ORDER BY {LessonOrder};
            """,
            [("$slug", slug)],
            r => new LessonSummary(
                r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
                r.GetInt32(5), r.GetInt32(6), DeviceJson.Tags(r.GetString(7))),
            cancellationToken).ConfigureAwait(false);
        return new ModuleDetail(module.Slug, module.Title, module.Summary, lessons, module.Map);
    }

    public async Task<LessonDetail?> GetLessonAsync(string id, CancellationToken cancellationToken) =>
        (await QueryAsync(
            $"""
            SELECT x.Id, x.ModuleSlug, m.Title, x.Slug, x.Title, x.Summary, x.SortOrder, x.ReadingMinutes, x.TagsJson,
                   x.BodyMarkdown, x.OutlineJson, x.CheatSheetTitle,
                   x.PrevId, x.PrevSlug, x.PrevTitle, x.NextId, x.NextSlug, x.NextTitle, x.TermsJson
            FROM (
                SELECT l.*,
                       LAG(l.Id) OVER w AS PrevId, LAG(l.Slug) OVER w AS PrevSlug, LAG(l.Title) OVER w AS PrevTitle,
                       LEAD(l.Id) OVER w AS NextId, LEAD(l.Slug) OVER w AS NextSlug, LEAD(l.Title) OVER w AS NextTitle
                FROM Lessons l
                WHERE l.ModuleSlug = (SELECT ModuleSlug FROM Lessons WHERE Id = $id)
                WINDOW w AS (ORDER BY {LessonOrder})
            ) x
            JOIN Modules m ON m.Slug = x.ModuleSlug
            WHERE x.Id = $id;
            """,
            [("$id", id)],
            r => new LessonDetail(
                r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
                r.GetInt32(6), r.GetInt32(7), DeviceJson.Tags(r.GetString(8)), r.GetString(9),
                DeviceJson.Outline(r.GetString(10)), r.GetString(11),
                Link(r, 12, r.GetString(1)), Link(r, 15, r.GetString(1)), DeviceJson.Terms(r.GetString(18))),
            cancellationToken).ConfigureAwait(false)).SingleOrDefault();

    public Task<IReadOnlyList<CheatSheetSummary>> GetCheatSheetsAsync(string? moduleSlug, CancellationToken cancellationToken) =>
        QueryAsync(
            $"""
            SELECT l.Id, l.ModuleSlug, m.Title, l.Slug, l.Title, l.CheatSheetTitle
            FROM Lessons l JOIN Modules m ON m.Slug = l.ModuleSlug
            WHERE $module IS NULL OR l.ModuleSlug = $module
            ORDER BY {ModuleOrder}, {LessonOrder};
            """,
            [("$module", moduleSlug)],
            r => new CheatSheetSummary(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5)),
            cancellationToken);

    public async Task<CheatSheetDetail?> GetCheatSheetAsync(string lessonId, CancellationToken cancellationToken) =>
        (await QueryAsync(
            """
            SELECT l.Id, l.ModuleSlug, m.Title, l.Slug, l.Title, l.CheatSheetTitle, l.CheatSheetMarkdown
            FROM Lessons l JOIN Modules m ON m.Slug = l.ModuleSlug
            WHERE l.Id = $id;
            """,
            [("$id", lessonId)],
            r => new CheatSheetDetail(
                r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6)),
            cancellationToken).ConfigureAwait(false)).SingleOrDefault();

    private static LessonLink? Link(SqliteDataReader reader, int first, string moduleSlug) =>
        reader.IsDBNull(first)
            ? null
            : new LessonLink(reader.GetString(first), moduleSlug, reader.GetString(first + 1), reader.GetString(first + 2));

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        (string Name, object? Value)[] parameters,
        Func<SqliteDataReader, T> map,
        CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // SQL text is constant in this class; values are always bound as parameters.
        command.CommandText = sql;
#pragma warning restore CA2100
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(map(reader));
        }

        return rows;
    }
}
