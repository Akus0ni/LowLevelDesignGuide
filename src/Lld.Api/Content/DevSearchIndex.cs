using Lld.Content;
using Lld.Contracts;
using Lld.Domain.Notes;
using Lld.Search.Sqlite;
using Microsoft.Data.Sqlite;

namespace Lld.Api.Content;

/// <summary>
/// Browser dev mode's search (spec 009 §3.8, plan §2.7): the same <see cref="SqliteSearchIndex"/> the device uses, over
/// a private in-memory database. Content is (re)loaded when the dev content's version changes; the caller's notes are
/// written into its own <c>Notes</c> table before each search, so the note triggers index them exactly as on a device.
/// One search at a time: this is a developer's tool, not a service.
/// </summary>
internal sealed class DevSearchIndex : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString;
    private readonly SqliteSearchIndex _index;
    private string? _loadedVersion;

    public DevSearchIndex()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"lld-dev-search-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        using var create = _keepAlive.CreateCommand();
        create.CommandText = SearchSchema.TablesSql + SearchSchema.DevNotesTableSql + SearchSchema.NoteTriggersSql;
        create.ExecuteNonQuery();
        _index = new SqliteSearchIndex(OpenAsync);
    }

    /// <param name="notes">The signed-in learner's notes, or none for a guest (spec Q7).</param>
    public async Task<SearchResultsDto> SearchAsync(
        ContentPack pack, string ownerKey, IReadOnlyList<Note> notes, SearchRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(notes);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = (SqliteTransaction)await _keepAlive.BeginTransactionAsync(cancellationToken);
            if (_loadedVersion != pack.Version)
            {
                await SearchContentWriter.ReplaceContentAsync(_keepAlive, transaction, pack, cancellationToken);
            }

            await ReplaceNotesAsync(transaction, ownerKey, notes, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _loadedVersion = pack.Version;
            return await _index.SearchAsync(ownerKey, request, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
        _gate.Dispose();
    }

    private async Task ReplaceNotesAsync(SqliteTransaction transaction, string ownerKey, IReadOnlyList<Note> notes, CancellationToken cancellationToken)
    {
        await using var clear = _keepAlive.CreateCommand();
        clear.Transaction = transaction;
        clear.CommandText = "DELETE FROM Notes;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        await using var insert = _keepAlive.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO Notes (Owner, Id, LessonId, Anchor, Body, DeletedAt) VALUES ($owner, $id, $lesson, $anchor, $body, NULL);";
        foreach (var note in notes.Where(n => !n.IsDeleted))
        {
            insert.Parameters.Clear();
            insert.Parameters.AddWithValue("$owner", ownerKey);
            insert.Parameters.AddWithValue("$id", note.Id.ToString());
            insert.Parameters.AddWithValue("$lesson", note.LessonId);
            insert.Parameters.AddWithValue("$anchor", note.Anchor);
            insert.Parameters.AddWithValue("$body", note.Body);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
