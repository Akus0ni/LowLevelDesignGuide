using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data.Schema;

/// <summary>
/// Brings the device database to the latest schema. The current version lives in SQLite's own
/// <c>PRAGMA user_version</c>, so a normal launch costs one read. Each step runs in its own transaction together with
/// the version bump: a failing step leaves the database at the previous version.
/// </summary>
public sealed class SchemaMigrator
{
    private readonly DeviceDatabase _database;
    private readonly IReadOnlyList<ISchemaMigration> _migrations;

    public SchemaMigrator(DeviceDatabase database, IEnumerable<ISchemaMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        _database = database;
        _migrations = [.. migrations.OrderBy(m => m.Version)];
        for (var i = 0; i < _migrations.Count; i++)
        {
            if (_migrations[i].Version != i + 1)
            {
                throw new ArgumentException("Schema steps must be numbered 1, 2, 3… with no gaps or repeats.", nameof(migrations));
            }
        }
    }

    /// <summary>Every step the app knows, in order. A new step is a new class added here.</summary>
    public static IReadOnlyList<ISchemaMigration> All { get; } = [new V1ContentTables()];

    public int LatestVersion => _migrations.Count;

    /// <returns>How many steps were applied (0 when the database was already current).</returns>
    /// <exception cref="InvalidOperationException">The database was written by a newer app version.</exception>
    public async Task<int> MigrateAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await ExecuteAsync(connection, "PRAGMA journal_mode = WAL;", cancellationToken).ConfigureAwait(false);
        var current = await ReadVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (current > LatestVersion)
        {
            throw new InvalidOperationException(
                $"The device database is at schema version {current}, newer than this app's {LatestVersion}.");
        }

        var pending = _migrations.Where(m => m.Version > current).ToList();
        foreach (var migration in pending)
        {
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, migration.Sql, cancellationToken, transaction).ConfigureAwait(false);
            await ExecuteAsync(connection, $"PRAGMA user_version = {migration.Version.ToString(CultureInfo.InvariantCulture)};", cancellationToken, transaction)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return pending.Count;
    }

    public static async Task<int> ReadVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection, string sql, CancellationToken cancellationToken, SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
#pragma warning disable CA2100 // Fixed schema SQL from this assembly; the only number is an int.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
