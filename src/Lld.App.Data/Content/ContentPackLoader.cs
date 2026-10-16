using Lld.Content;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data.Content;

/// <param name="Loaded"><c>true</c> when content was (re)loaded; <c>false</c> when it was already current.</param>
public sealed record ContentLoadResult(bool Loaded, string Version);

/// <summary>
/// Puts the bundled content into the device database. On a normal launch it compares two short strings and stops.
/// When the app ships new content it replaces every content row in one transaction: a failure leaves the previous
/// content (and its version) in place, so the app never shows half a pack.
/// </summary>
public sealed class ContentPackLoader(DeviceDatabase database)
{
    public const string VersionKey = "content.version";

    public async Task<ContentLoadResult> EnsureLoadedAsync(IContentPackSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var bundled = (await source.ReadVersionAsync(cancellationToken).ConfigureAwait(false)).Trim();
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (await ReadLoadedVersionAsync(connection, cancellationToken).ConfigureAwait(false) == bundled)
        {
            return new ContentLoadResult(Loaded: false, bundled);
        }

        ContentPack pack;
        await using (var stream = await source.OpenPackAsync(cancellationToken).ConfigureAwait(false))
        {
            pack = await ContentPackJson.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        if (pack.Version != bundled)
        {
            throw new InvalidDataException($"The bundled pack is version {pack.Version} but its version file says {bundled}.");
        }

        await ReplaceAsync(connection, pack, cancellationToken).ConfigureAwait(false);
        return new ContentLoadResult(Loaded: true, pack.Version);
    }

    public static async Task<string?> ReadLoadedVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM DeviceMeta WHERE Key = $key;";
        command.Parameters.AddWithValue("$key", VersionKey);
        return (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReplaceAsync(SqliteConnection connection, ContentPack pack, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (var clear = Command(connection, transaction, "DELETE FROM Lessons; DELETE FROM Modules;"))
        {
            await clear.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var insertModule = Command(connection, transaction,
            "INSERT INTO Modules (Slug, Title, Summary, SortOrder, Map) VALUES ($slug, $title, $summary, $order, $map);");
        await using var insertLesson = Command(connection, transaction, """
            INSERT INTO Lessons (Id, ModuleSlug, Slug, Title, Summary, SortOrder, TagsJson, Source, ReadingMinutes,
                                 BodyMarkdown, OutlineJson, CheatSheetTitle, CheatSheetMarkdown, TermsJson)
            VALUES ($id, $module, $slug, $title, $summary, $order, $tags, $source, $minutes,
                    $body, $outline, $sheetTitle, $sheetBody, $terms);
            """);
        foreach (var module in pack.Modules)
        {
            Bind(insertModule,
                ("$slug", module.Slug), ("$title", module.Title), ("$summary", module.Summary), ("$order", module.Order), ("$map", module.Map));
            await insertModule.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            foreach (var lesson in module.Lessons)
            {
                Bind(insertLesson,
                    ("$id", lesson.Id), ("$module", module.Slug), ("$slug", lesson.Slug), ("$title", lesson.Title),
                    ("$summary", lesson.Summary), ("$order", lesson.Order), ("$tags", DeviceJson.Tags(lesson.Tags)),
                    ("$source", lesson.Source), ("$minutes", lesson.ReadingMinutes), ("$body", lesson.BodyMarkdown),
                    ("$outline", DeviceJson.Outline(lesson.Outline)), ("$sheetTitle", lesson.CheatSheet.Title),
                    ("$sheetBody", lesson.CheatSheet.BodyMarkdown), ("$terms", DeviceJson.Terms(lesson.Terms)));
                await insertLesson.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await using (var version = Command(connection, transaction,
            "INSERT INTO DeviceMeta (Key, Value) VALUES ($key, $value) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value;"))
        {
            Bind(version, ("$key", VersionKey), ("$value", pack.Version));
            await version.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void Bind(SqliteCommand command, params (string Name, object? Value)[] parameters)
    {
        command.Parameters.Clear();
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}
