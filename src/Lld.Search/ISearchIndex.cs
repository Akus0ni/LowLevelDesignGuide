using Lld.Contracts;

namespace Lld.Search;

/// <summary>
/// Full-text search over content and one owner's notes (spec 009, port). The device and the API's dev mode use
/// <see cref="Sqlite.SqliteSearchIndex"/> over different databases; both pass the same contract tests.
/// </summary>
public interface ISearchIndex
{
    /// <param name="ownerKey">Whose notes may appear (a <c>ProgressOwner</c> key); content is everyone's.</param>
    Task<SearchResultsDto> SearchAsync(string ownerKey, SearchRequestDto request, CancellationToken cancellationToken);
}
