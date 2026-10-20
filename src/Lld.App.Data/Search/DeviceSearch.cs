using Lld.App.Data.Progress;
using Lld.Contracts;
using Lld.Search.Sqlite;

namespace Lld.App.Data.Search;

/// <summary>
/// Search on the device (spec 009 §3.8): the device database's index, with the owner resolved here (never sent by
/// JavaScript), so a learner only ever finds their own notes. No network.
/// </summary>
public sealed class DeviceSearch(DeviceDatabase database, IProgressOwnerSource owners)
{
    private readonly SqliteSearchIndex _index = new(database.OpenAsync);

    public async Task<SearchResultsDto> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        return await _index.SearchAsync(owner.Key, request, cancellationToken).ConfigureAwait(false);
    }
}
