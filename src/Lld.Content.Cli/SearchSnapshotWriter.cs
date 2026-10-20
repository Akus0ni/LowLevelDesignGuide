using System.Text;
using System.Text.Json;
using Lld.Contracts;
using Lld.Search.Sqlite;
using Microsoft.Data.Sqlite;

namespace Lld.Content.Cli;

/// <summary>
/// Writes <c>/api/search</c> responses for a fixed list of requests (spec 009 T14), from the same engine the app and
/// the dev API use, so the Playwright tests search real content without a running API. Signed out: no notes.
/// <code>
/// GET /api/search?q=&amp;kind=&amp;module=&amp;lessonId= → search/{key}.json
/// key = base64url(UTF-8 of q, kind, module, lessonId joined by U+0001; a missing value is empty)
/// </code>
/// <c>web/e2e/content.ts</c> computes the same key.
/// </summary>
internal static class SearchSnapshotWriter
{
    private const string Folder = "search";
    private const string GuestOwner = "guest";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <returns>The number of files written.</returns>
    public static async Task<int> WriteAsync(ContentPack pack, FileInfo requestsFile, DirectoryInfo snapshot, CancellationToken cancellationToken)
    {
        var requests = JsonSerializer.Deserialize<List<SearchRequestDto>>(await File.ReadAllTextAsync(requestsFile.FullName, cancellationToken), Json)
            ?? [];
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"lld-snapshot-search-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(cancellationToken);
        await using (var create = keepAlive.CreateCommand())
        {
            create.CommandText = SearchSchema.TablesSql + SearchSchema.DevNotesTableSql;
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        await SearchContentWriter.ReplaceContentAsync(keepAlive, null, pack, cancellationToken);
        var index = new SqliteSearchIndex(async ct =>
        {
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(ct);
            return connection;
        });

        var folder = Directory.CreateDirectory(Path.Combine(snapshot.FullName, Folder));
        foreach (var request in requests)
        {
            var results = await index.SearchAsync(GuestOwner, request with { Limit = SqliteSearchIndex.MaxLimit }, cancellationToken);
            await using var stream = File.Create(Path.Combine(folder.FullName, $"{KeyOf(request)}.json"));
            await JsonSerializer.SerializeAsync(stream, results, Json, cancellationToken);
        }

        return requests.Count;
    }

    public static string KeyOf(SearchRequestDto request)
    {
        var joined = string.Join('\u0001', request.Query, request.Kind ?? string.Empty, request.Module ?? string.Empty, request.LessonId ?? string.Empty);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(joined)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
