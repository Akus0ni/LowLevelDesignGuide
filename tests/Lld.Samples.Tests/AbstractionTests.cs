using Lld.Samples.OopSolid.Abstraction.After;
using Lld.Samples.OopSolid.Abstraction.Language;

namespace Lld.Samples.Tests;

/// <summary>What the Abstraction lesson claims about each design.</summary>
public sealed class AbstractionTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("lld-samples-").FullName;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task SalesReport_PublishesTotalsByRegion_ThroughAnyStore()
    {
        var store = new InMemoryDocumentStore();
        var report = new SalesReport(store);

        await report.PublishAsync([new("North", 10), new("South", 5), new("North", 2.5m)], new DateOnly(2026, 10, 1), Ct);

        (await store.LoadAsync("sales-2026-10.csv", Ct)).Should().Be("Region,Total\nNorth,12.5\nSouth,5\n");
    }

    [Fact]
    public void Before_SalesReport_WritesTheSameFile_ButOnlyToAFolder()
    {
        Lld.Samples.OopSolid.Abstraction.Before.SalesReport.Publish(
            [new("North", 10), new("South", 5), new("North", 2.5m)], new DateTime(2026, 10, 1), _folder);

        File.ReadAllText(Path.Combine(_folder, "sales-2026-10.csv")).Should().Be("Region,Total\nNorth,12.5\nSouth,5\n");
    }

    public static TheoryData<string> Stores => new() { "folder", "memory" };

    /// <summary>Both stores keep the same promise, so callers can't tell them apart.</summary>
    [Theory]
    [MemberData(nameof(Stores))]
    public async Task EveryStore_LoadsWhatWasSaved_AndNullForUnknownNames(string kind)
    {
        IDocumentStore store = kind == "folder" ? new FolderDocumentStore(_folder) : new InMemoryDocumentStore();

        await store.SaveAsync("a.csv", "one", Ct);
        await store.SaveAsync("a.csv", "two", Ct);

        (await store.LoadAsync("a.csv", Ct)).Should().Be("two");
        (await store.LoadAsync("missing.csv", Ct)).Should().BeNull();
    }

    [Fact]
    public async Task FolderStore_KeepsNamesInsideItsFolder()
    {
        var store = new FolderDocumentStore(_folder);

        await store.SaveAsync("../escape.csv", "x", Ct);

        File.Exists(Path.Combine(_folder, "escape.csv")).Should().BeTrue();
    }

    [Fact]
    public void LazySequence_RunsItsWorkAgainForEveryEnumeration()
    {
        var directory = new CustomerDirectory([new("Asha", "Pune"), new("Ben", "Leeds")]);

        var inPune = directory.InCity("Pune");
        _ = inPune.Count();
        _ = inPune.ToList();

        directory.Evaluations.Should().Be(4, "two customers, enumerated twice");
    }

    [Fact]
    public void FinishedList_DoesTheWorkOnce()
    {
        var directory = new CustomerDirectory([new("Asha", "Pune"), new("Ben", "Leeds")]);

        var inPune = directory.ListInCity("Pune");
        _ = inPune.Count;
        _ = inPune.ToList();

        directory.Evaluations.Should().Be(2);
    }
}
