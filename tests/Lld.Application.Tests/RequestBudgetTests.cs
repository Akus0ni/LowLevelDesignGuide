using FluentAssertions;
using Lld.Application.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Lld.Application.Tests;

public sealed class RequestBudgetTests
{
    private static DateTimeOffset Utc(int year, int month, int day, int hour = 0) => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, "2026-10-15", "2026-10-01", "2026-11-01")]
    [InlineData(20, "2026-10-15", "2026-09-20", "2026-10-20")]
    [InlineData(20, "2026-10-20", "2026-10-20", "2026-11-20")]
    [InlineData(31, "2026-02-28", "2026-02-28", "2026-03-28")] // clamped to 28
    [InlineData(5, "2026-01-02", "2025-12-05", "2026-01-05")]
    public void Window_RunsFromResetDayToTheNextOne(int resetDay, string instant, string start, string resetsAt)
    {
        var window = BudgetWindow.Containing(DateTimeOffset.Parse(instant + "T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture), resetDay);

        window.Key.Should().Be(start);
        window.ResetsAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).Should().Be(resetsAt);
    }

    [Fact]
    public void Usage_IsNearLimit_AtTheThresholdOfEitherLimit()
    {
        var limits = new BudgetOptions { MonthlyRequests = 1000, MonthlyBytes = 10_000, ThresholdPercent = 95 };
        var window = BudgetWindow.Containing(Utc(2026, 10, 15), 1);

        new BudgetUsage(window, 949, 0, limits).IsNearLimit.Should().BeFalse();
        new BudgetUsage(window, 950, 0, limits).IsNearLimit.Should().BeTrue();
        new BudgetUsage(window, 0, 9_500, limits).IsNearLimit.Should().BeTrue();
    }

    [Fact]
    public void Meter_CountsRequestsAndBytesWithHeaderAllowance()
    {
        var meter = new RequestBudgetMeter(new BudgetOptions { HeaderAllowanceBytes = 100 }, new FakeTimeProvider(Utc(2026, 10, 15)));
        meter.Load(requests: 10, bytes: 5_000);

        meter.Record(400);
        meter.Record(0);

        meter.Usage.Requests.Should().Be(12);
        meter.Usage.Bytes.Should().Be(5_000 + 500 + 100);
    }

    [Fact]
    public void Meter_TakePending_ReturnsOnlyUnsavedCountsOnce()
    {
        var meter = new RequestBudgetMeter(new BudgetOptions { HeaderAllowanceBytes = 0 }, new FakeTimeProvider(Utc(2026, 10, 15)));
        meter.Load(requests: 10, bytes: 5_000);
        meter.Record(50);

        var first = meter.TakePending();
        var second = meter.TakePending();

        first.Requests.Should().Be(1);
        first.Bytes.Should().Be(50);
        first.Window.Key.Should().Be("2026-10-01");
        second.Requests.Should().Be(0);
        meter.Usage.Requests.Should().Be(11);
    }

    [Fact]
    public void Meter_RollsToANewWindowAfterTheResetDay()
    {
        var time = new FakeTimeProvider(Utc(2026, 10, 31, 23));
        var meter = new RequestBudgetMeter(new BudgetOptions(), time);
        meter.Record(1_000);
        meter.RollIfNewWindow().Should().BeFalse();

        time.Advance(TimeSpan.FromHours(2));

        meter.RollIfNewWindow().Should().BeTrue();
        meter.Window.Key.Should().Be("2026-11-01");
        meter.Usage.Requests.Should().Be(0);
    }

    [Fact]
    public async Task GetBudgetUsage_ReadsTheCurrentWindowFromTheStore()
    {
        var store = new InMemoryUsageStore();
        await store.AddAsync("2026-10-01", 19_000, 1_000, CancellationToken.None);
        await store.AddAsync("2026-09-01", 5, 5, CancellationToken.None);
        var useCase = new GetBudgetUsage(store, new BudgetOptions(), new FakeTimeProvider(Utc(2026, 10, 15)));

        var usage = await useCase.ExecuteAsync(CancellationToken.None);

        usage.Requests.Should().Be(19_000);
        usage.RequestsPercent.Should().Be(95);
        usage.IsNearLimit.Should().BeTrue();
    }

    private sealed class InMemoryUsageStore : IUsageCounterStore
    {
        private readonly Dictionary<string, (long Requests, long Bytes)> _rows = [];

        public Task AddAsync(string windowKey, long requests, long bytes, CancellationToken cancellationToken)
        {
            var (r, b) = _rows.GetValueOrDefault(windowKey);
            _rows[windowKey] = (r + requests, b + bytes);
            return Task.CompletedTask;
        }

        public Task<(long Requests, long Bytes)> GetAsync(string windowKey, CancellationToken cancellationToken) =>
            Task.FromResult(_rows.GetValueOrDefault(windowKey));
    }
}
