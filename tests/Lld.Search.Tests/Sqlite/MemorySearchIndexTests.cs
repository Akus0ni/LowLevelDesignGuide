using Lld.Content;
using Lld.Search.Sqlite;
using Microsoft.Data.Sqlite;

namespace Lld.Search.Tests.Sqlite;

/// <summary>The contract over a private in-memory database with the dev <c>Notes</c> table and the note triggers.</summary>
public sealed class MemorySearchIndexTests : SearchIndexContract
{
    protected override async Task<ISearchFixture> CreateAsync() => await MemoryFixture.CreateAsync();

    private sealed class MemoryFixture : ISearchFixture
    {
        private readonly SqliteConnection _keepAlive;
        private readonly string _connectionString;

        private MemoryFixture(SqliteConnection keepAlive, string connectionString)
        {
            _keepAlive = keepAlive;
            _connectionString = connectionString;
            Index = new SqliteSearchIndex(OpenAsync);
        }

        public ISearchIndex Index { get; }

        public static async Task<MemoryFixture> CreateAsync()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"search-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
            }.ToString();
            var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();
            await ExecuteAsync(keepAlive, SearchSchema.TablesSql + SearchSchema.DevNotesTableSql + SearchSchema.NoteTriggersSql);
            return new MemoryFixture(keepAlive, connectionString);
        }

        public async Task LoadContentAsync(ContentPack pack)
        {
            await using var transaction = (SqliteTransaction)await _keepAlive.BeginTransactionAsync();
            await SearchContentWriter.ReplaceContentAsync(_keepAlive, transaction, pack, TestContext.Current.CancellationToken);
            await transaction.CommitAsync();
        }

        public async Task SaveNoteAsync(string ownerKey, Guid id, string lessonId, string anchor, string body, bool deleted = false)
        {
            await using var command = _keepAlive.CreateCommand();
            command.CommandText = """
                INSERT INTO Notes (Owner, Id, LessonId, Anchor, Body, DeletedAt) VALUES ($owner, $id, $lesson, $anchor, $body, $deleted)
                ON CONFLICT (Owner, Id) DO UPDATE SET Anchor = excluded.Anchor, Body = excluded.Body, DeletedAt = excluded.DeletedAt;
                """;
            command.Parameters.AddWithValue("$owner", ownerKey);
            command.Parameters.AddWithValue("$id", id.ToString());
            command.Parameters.AddWithValue("$lesson", lessonId);
            command.Parameters.AddWithValue("$anchor", anchor);
            command.Parameters.AddWithValue("$body", body);
            command.Parameters.AddWithValue("$deleted", deleted ? "2026-10-21T00:00:00Z" : DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync() => await _keepAlive.DisposeAsync();

        private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static async Task ExecuteAsync(SqliteConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
}
