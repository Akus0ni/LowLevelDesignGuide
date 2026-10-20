namespace Lld.Search.Tests.Sqlite;

/// <summary>The contract over an in-memory database with the dev <c>Notes</c> table and the note triggers.</summary>
public sealed class MemorySearchIndexTests : SearchIndexContract
{
    protected override async Task<ISearchFixture> CreateAsync() => await MemorySearchFixture.CreateAsync();
}
