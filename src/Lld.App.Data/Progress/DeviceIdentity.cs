using System.Globalization;

namespace Lld.App.Data.Progress;

/// <summary>
/// This installation's id, stamped on every progress record so spec 010's sync can tell devices apart. Created once
/// and kept in <c>DeviceMeta</c>; it changes only if the app's data is cleared.
/// </summary>
public sealed class DeviceIdentity(DeviceDatabase database)
{
    public const string Key = "device.id";

    private string? _id;

    /// <summary>Two first calls at once agree without a lock: <c>INSERT OR IGNORE</c> keeps whichever id was stored first.</summary>
    public async Task<string> GetIdAsync(CancellationToken cancellationToken) =>
        _id ??= await ReadOrCreateAsync(cancellationToken).ConfigureAwait(false);

    private async Task<string> ReadOrCreateAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO DeviceMeta (Key, Value) VALUES ($key, $candidate);
            SELECT Value FROM DeviceMeta WHERE Key = $key;
            """;
        command.Parameters.AddWithValue("$key", Key);
        command.Parameters.AddWithValue("$candidate", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        return (string)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }
}
