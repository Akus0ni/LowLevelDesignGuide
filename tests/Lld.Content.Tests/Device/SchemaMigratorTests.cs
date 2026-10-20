using FluentAssertions;
using Lld.App.Data.Schema;
using Microsoft.Data.Sqlite;

namespace Lld.Content.Tests.Device;

public sealed class SchemaMigratorTests : IDisposable
{
    private readonly TestDeviceDatabase _device = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _device.Dispose();

    [Fact]
    public async Task FreshDatabase_GetsEveryStep_AndWalMode()
    {
        var applied = await _device.MigrateAsync(Ct);

        applied.Should().Be(SchemaMigrator.All.Count);
        await using var connection = await _device.Database.OpenAsync(Ct);
        (await SchemaMigrator.ReadVersionAsync(connection, Ct)).Should().Be(SchemaMigrator.All.Count);
        (await Scalar(connection, "PRAGMA journal_mode;")).Should().Be("wal");
        (await Scalar(connection, "SELECT group_concat(name, ',') FROM (SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name);"))
            .Should().Be("ActivityDays,DeviceMeta,LessonProgress,Lessons,Modules");
    }

    [Fact]
    public async Task CurrentDatabase_AppliesNothing()
    {
        await _device.MigrateAsync(Ct);

        (await _device.MigrateAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task OlderDatabase_GetsOnlyTheNewSteps_InOrder()
    {
        await new SchemaMigrator(_device.Database, [new V1ContentTables()]).MigrateAsync(Ct);

        var applied = await new SchemaMigrator(_device.Database, [new V1ContentTables(), new Step(2, "CREATE TABLE Notes (Id TEXT);")])
            .MigrateAsync(Ct);

        applied.Should().Be(1);
        await using var connection = await _device.Database.OpenAsync(Ct);
        (await SchemaMigrator.ReadVersionAsync(connection, Ct)).Should().Be(2);
    }

    [Fact]
    public async Task LaterSteps_UpgradeADeviceThatAlreadyHasV1Content()
    {
        await new SchemaMigrator(_device.Database, [new V1ContentTables()]).MigrateAsync(Ct);
        await using (var connection = await _device.Database.OpenAsync(Ct))
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO Modules VALUES ('m', 'M', 'S', 0);
                INSERT INTO Lessons (Id, ModuleSlug, Slug, Title, Summary, SortOrder, TagsJson, Source, ReadingMinutes,
                                     BodyMarkdown, OutlineJson, CheatSheetTitle, CheatSheetMarkdown)
                VALUES ('l', 'm', 'l', 'L', 'S', 1, '[]', NULL, 1, '# L', '[]', 'C', '# C');
                """;
            await insert.ExecuteNonQueryAsync(Ct);
        }

        (await _device.MigrateAsync(Ct)).Should().Be(SchemaMigrator.All.Count - 1);

        await using var check = await _device.Database.OpenAsync(Ct);
        (await Scalar(check, "SELECT TermsJson FROM Lessons WHERE Id = 'l';")).Should().Be("[]");
        (await Scalar(check, "SELECT coalesce(MapJson, 'none') FROM Modules WHERE Slug = 'm';")).Should().Be("none");
    }

    [Fact]
    public async Task V4_AddsEmptyProgressTablesToAV3Database()
    {
        await new SchemaMigrator(_device.Database, [new V1ContentTables(), new V2LessonTerms(), new V3ModuleMap()]).MigrateAsync(Ct);

        (await _device.MigrateAsync(Ct)).Should().Be(SchemaMigrator.All.Count - 3);

        await using var connection = await _device.Database.OpenAsync(Ct);
        (await Scalar(connection, "SELECT count(*) FROM LessonProgress;")).Should().Be("0");
        (await Scalar(connection, "SELECT count(*) FROM ActivityDays;")).Should().Be("0");
    }

    [Fact]
    public async Task V5_AddsPatternColumns_AndKeepsProgress()
    {
        await new SchemaMigrator(_device.Database, [new V1ContentTables(), new V2LessonTerms(), new V3ModuleMap(), new V4Progress()]).MigrateAsync(Ct);
        await using (var connection = await _device.Database.OpenAsync(Ct))
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO Modules (Slug, Title, Summary, SortOrder) VALUES ('m', 'M', 'S', 0);
                INSERT INTO LessonProgress (Owner, LessonId, Status, UpdatedAt, DeviceId) VALUES ('guest', 'l', 1, '2026-10-20T00:00:00Z', 'd');
                """;
            await insert.ExecuteNonQueryAsync(Ct);
        }

        (await _device.MigrateAsync(Ct)).Should().Be(1);

        await using var check = await _device.Database.OpenAsync(Ct);
        (await Scalar(check, "SELECT coalesce(PickerJson, 'none') || ' ' || LookalikesJson FROM Modules WHERE Slug = 'm';")).Should().Be("none []");
        (await Scalar(check, "SELECT count(*) FROM LessonProgress;")).Should().Be("1");
    }

    [Fact]
    public async Task FailingStep_LeavesThePreviousVersion_AndNoHalfDoneChanges()
    {
        var migrator = new SchemaMigrator(_device.Database,
            [new V1ContentTables(), new Step(2, "CREATE TABLE Notes (Id TEXT); THIS IS NOT SQL;")]);

        var migrate = () => migrator.MigrateAsync(Ct);

        await migrate.Should().ThrowAsync<SqliteException>();
        await using var connection = await _device.Database.OpenAsync(Ct);
        (await SchemaMigrator.ReadVersionAsync(connection, Ct)).Should().Be(1);
        (await Scalar(connection, "SELECT count(*) FROM sqlite_master WHERE name = 'Notes';")).Should().Be("0");
    }

    [Fact]
    public async Task DatabaseFromANewerApp_IsRefused()
    {
        await new SchemaMigrator(_device.Database, [new V1ContentTables(), new Step(2, "SELECT 1;")]).MigrateAsync(Ct);

        var migrate = () => new SchemaMigrator(_device.Database, [new V1ContentTables()]).MigrateAsync(Ct);

        await migrate.Should().ThrowAsync<InvalidOperationException>().WithMessage("*version 2, newer than this app's 1*");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    public void StepNumbers_MustBeOneTwoThree(params int[] versions)
    {
        var create = () => new SchemaMigrator(_device.Database, versions.Select(v => new Step(v, "SELECT 1;")));

        create.Should().Throw<ArgumentException>();
    }

    private static async Task<string?> Scalar(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(Ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record Step(int Version, string Sql) : ISchemaMigration
    {
        public string Name => $"Test step {Version}";
    }
}
