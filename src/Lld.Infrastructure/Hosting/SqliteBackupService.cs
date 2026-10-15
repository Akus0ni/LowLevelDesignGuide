using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Lld.Infrastructure.Hosting;

public sealed record BackupFile(string Path, DateTimeOffset TakenAt, long SizeBytes);

public sealed record BackupResult(BackupFile Created, IReadOnlyList<BackupFile> Pruned);

/// <summary>
/// Copies the live database with <c>VACUUM INTO</c>: a consistent snapshot even while the API is writing
/// (WAL mode), compacted, in one file. Keeps the last <see cref="BackupOptions.KeepDays"/> days.
/// </summary>
public sealed class SqliteBackupService(DatabaseOptions database, BackupOptions options, TimeProvider time)
{
    private const string FilePrefix = "lld-";
    private const string FileExtension = ".db";
    private const string TimestampFormat = "yyyyMMdd-HHmm";

    public async Task<BackupResult> RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.Path);
        var takenAt = time.GetLocalNow();
        var target = Path.Combine(options.Path, FileNameFor(takenAt));
        if (File.Exists(target))
        {
            File.Delete(target); // a second run in the same minute replaces the first
        }

        await using (var connection = new SqliteConnection(database.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "VACUUM INTO $target;";
            command.Parameters.AddWithValue("$target", target);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var created = new BackupFile(target, takenAt, new FileInfo(target).Length);
        return new BackupResult(created, Prune(takenAt));
    }

    /// <summary>Kept backups, newest first.</summary>
    public IReadOnlyList<BackupFile> List()
    {
        if (!Directory.Exists(options.Path))
        {
            return [];
        }

        return Directory.EnumerateFiles(options.Path, FilePrefix + "*" + FileExtension)
            .Select(path => TryParse(path, out var takenAt) ? new BackupFile(path, takenAt, new FileInfo(path).Length) : null)
            .OfType<BackupFile>()
            .OrderByDescending(b => b.TakenAt)
            .ToList();
    }

    private List<BackupFile> Prune(DateTimeOffset now)
    {
        var cutoff = now.AddDays(-options.KeepDays);
        var expired = List().Where(b => b.TakenAt < cutoff).ToList();
        foreach (var backup in expired)
        {
            File.Delete(backup.Path);
        }

        return expired;
    }

    private static string FileNameFor(DateTimeOffset takenAt) =>
        FilePrefix + takenAt.ToString(TimestampFormat, CultureInfo.InvariantCulture) + FileExtension;

    private bool TryParse(string path, out DateTimeOffset takenAt)
    {
        takenAt = default;
        var name = Path.GetFileNameWithoutExtension(path);
        if (!DateTime.TryParseExact(name[FilePrefix.Length..], TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return false;
        }

        takenAt = new DateTimeOffset(local, time.LocalTimeZone.GetUtcOffset(local));
        return true;
    }
}
