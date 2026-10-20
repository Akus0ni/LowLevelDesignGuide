using FluentAssertions;
using Lld.App.Data.Schema;
using Lld.Contracts;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Lld.Search.Tests.Device;

/// <summary>The search contract on the device database, plus what only the device does: upgrade, reload, adoption.</summary>
public sealed class DeviceSearchIndexTests : SearchIndexContract
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected override async Task<ISearchFixture> CreateAsync() => await DeviceSearchFixture.CreateAsync();

    /// <summary>
    /// Spec A12: a V6 database with notes upgrades to V7; its notes are searchable at once, and once the pack loads
    /// (each content reload re-indexes the notes) they carry their lesson and section names.
    /// </summary>
    [Fact]
    public async Task UpgradeFromV6_IndexesExistingNotes()
    {
        var v6 = SchemaMigrator.All.Where(m => m.Version <= 6).ToList();
        await using var fixture = await DeviceSearchFixture.CreateAsync(v6);
        await fixture.SaveNoteAsync("guest", Guid.NewGuid(), SearchTestPack.Decorator, "the-idea", "written before search existed");

        await new SchemaMigrator(fixture.Database, SchemaMigrator.All).MigrateAsync(Ct);
        var beforeContent = await NotesAsync(fixture, "guest", "existed");
        await fixture.LoadContentAsync(SearchTestPack.Create());
        var afterContent = await NotesAsync(fixture, "guest", "existed");

        beforeContent.Should().ContainSingle();
        afterContent.Should().ContainSingle().Which.Should().BeEquivalentTo(new { LessonTitle = "Decorator", Heading = "The idea" });
    }

    [Fact]
    public async Task AdoptingGuestNotes_MovesThemToTheUser()
    {
        await using var fixture = await DeviceSearchFixture.CreateAsync();
        await fixture.LoadContentAsync(SearchTestPack.Create());
        await fixture.SaveNoteAsync("guest", Guid.NewGuid(), SearchTestPack.Srp, "", "guest thought");

        (await fixture.Notes.AdoptGuestAsync(ProgressOwner.User("alice"), Ct)).IsSuccess.Should().BeTrue();

        (await NotesAsync(fixture, "user:alice", "thought")).Should().ContainSingle();
        (await NotesAsync(fixture, "guest", "thought")).Should().BeEmpty();
    }

    [Fact]
    public async Task ARolledBackNoteWrite_LeavesTheIndexAsItWas()
    {
        await using var fixture = await DeviceSearchFixture.CreateAsync();
        await fixture.LoadContentAsync(SearchTestPack.Create());
        var tooMany = Enumerable.Range(0, NoteLimits.MaxNotesPerLesson + 1)
            .Select(i => new Note(Guid.NewGuid(), SearchTestPack.Srp, "", $"overflow {i}", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null, "d"))
            .ToList();

        var saved = await fixture.Notes.SaveNotesAsync(ProgressOwner.Guest, tooMany, Ct);

        saved.IsFailure.Should().BeTrue();
        (await NotesAsync(fixture, "guest", "overflow")).Should().BeEmpty();
    }

    [Fact]
    public async Task TheSearchTablesUseFts5OnThisPlatform()
    {
        await using var fixture = await DeviceSearchFixture.CreateAsync();
        await using var connection = await fixture.Database.OpenAsync(Ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_compileoption_used('ENABLE_FTS5');";

        Convert.ToInt32(await command.ExecuteScalarAsync(Ct), System.Globalization.CultureInfo.InvariantCulture).Should().Be(1);
    }

    private static async Task<IReadOnlyList<SearchHitDto>> NotesAsync(DeviceSearchFixture fixture, string owner, string query) =>
        (await fixture.Index.SearchAsync(owner, new SearchRequestDto(query, SearchKinds.Note), Ct)).Hits;
}
