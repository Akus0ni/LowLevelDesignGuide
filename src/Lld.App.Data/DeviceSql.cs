using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data;

/// <summary>Small ADO.NET helpers shared by the device's row classes: commands with parameters, and dates as ISO-8601 UTC text.</summary>
internal static class DeviceSql
{
    public static SqliteCommand Command(
        SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        Bind(command, parameters);
        return command;
    }

    public static void Bind(SqliteCommand command, params (string Name, object? Value)[] parameters)
    {
        command.Parameters.Clear();
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    /// <summary>A date as UTC round-trip text, which sorts correctly as a string.</summary>
    public static string? Text(DateTimeOffset? value) =>
        value?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset? Time(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
