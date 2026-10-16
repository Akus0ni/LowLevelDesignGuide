using Microsoft.Data.Sqlite;

namespace Lld.App.Data;

/// <summary>
/// The device's own SQLite file. Hands out open connections with foreign keys on; pooling keeps reopening cheap.
/// Plain ADO.NET on purpose: EF Core cost 1–5 s per launch on Android (spec 003 plan §4).
/// </summary>
public sealed class DeviceDatabase
{
    public const string FileName = "lld-device.db";

    public DeviceDatabase(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            ForeignKeys = true,
            Pooling = true,
        }.ToString();
    }

    public string Path { get; }

    public string ConnectionString { get; }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
