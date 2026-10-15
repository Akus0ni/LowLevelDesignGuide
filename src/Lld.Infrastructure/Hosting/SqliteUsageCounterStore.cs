using Lld.Application.Hosting;
using Lld.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lld.Infrastructure.Hosting;

/// <summary>Persisted tunnel usage for one budget window.</summary>
public sealed class UsageCounter
{
    /// <summary>Window start date, e.g. <c>2026-10-01</c> (see <see cref="BudgetWindow.Key"/>).</summary>
    public string WindowStart { get; set; } = string.Empty;

    public long Requests { get; set; }

    public long Bytes { get; set; }
}

/// <summary>Adds counts with one atomic upsert, so the API's flusher and a restart never lose or double them.</summary>
internal sealed class SqliteUsageCounterStore(LldDbContext db) : IUsageCounterStore
{
    public Task AddAsync(string windowKey, long requests, long bytes, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "UsageCounters" ("WindowStart", "Requests", "Bytes") VALUES ({windowKey}, {requests}, {bytes})
            ON CONFLICT("WindowStart") DO UPDATE SET
                "Requests" = "Requests" + excluded."Requests",
                "Bytes" = "Bytes" + excluded."Bytes";
            """,
            cancellationToken);

    public async Task<(long Requests, long Bytes)> GetAsync(string windowKey, CancellationToken cancellationToken)
    {
        var counter = await db.UsageCounters.AsNoTracking().SingleOrDefaultAsync(c => c.WindowStart == windowKey, cancellationToken);
        return counter is null ? (0, 0) : (counter.Requests, counter.Bytes);
    }
}
