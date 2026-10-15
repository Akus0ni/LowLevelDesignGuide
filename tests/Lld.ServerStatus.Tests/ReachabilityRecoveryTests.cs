using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Lld.ServerStatus.Tests;

public sealed class ReachabilityRecoveryTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 18, 0, 0, TimeSpan.Zero));
    private readonly ServerStatusTracker _tracker;
    private int _probes;

    public ReachabilityRecoveryTests() => _tracker = new ServerStatusTracker(_time);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A probe that succeeds, as the app's HTTP pipeline would report it.</summary>
    private ReachabilityRecovery Recovery(bool serverAnswers = true) => new(_tracker, _ =>
    {
        _probes++;
        if (serverAnswers)
        {
            _tracker.ReportCallSucceeded();
        }
        else
        {
            _tracker.ReportCallFailed();
        }

        return Task.CompletedTask;
    }, _time);

    [Fact]
    public async Task PhoneLostItsConnection_ThenGotItBack_GoesOnlineWithOneProbe()
    {
        _tracker.Apply(StatusMessage.Create(ServerState.Online, StatusReasons.Startup, _time.GetUtcNow()));
        _tracker.ReportCallFailed(); // the request made with mobile data off
        var recovery = Recovery();

        (await recovery.TryRecoverAsync(Ct)).Should().BeTrue();

        _probes.Should().Be(1);
        _tracker.Current.State.Should().Be(ServerState.Online);
    }

    [Theory]
    [InlineData(StatusReasons.Shutdown)]
    [InlineData(StatusReasons.Quota)]
    [InlineData(StatusReasons.Manual)]
    public async Task SignedOffline_IsTheServersWord_AndIsNotProbed(string reason)
    {
        _tracker.Apply(StatusMessage.Create(ServerState.Offline, reason, _time.GetUtcNow()));

        (await Recovery().TryRecoverAsync(Ct)).Should().BeFalse();

        _probes.Should().Be(0);
    }

    [Fact]
    public async Task WhileOnline_NothingIsProbed()
    {
        _tracker.ReportCallSucceeded();

        (await Recovery().TryRecoverAsync(Ct)).Should().BeFalse();

        _probes.Should().Be(0);
    }

    [Fact]
    public async Task FlappingConnection_ProbesAtMostOncePerInterval()
    {
        _tracker.ReportCallFailed();
        var recovery = Recovery(serverAnswers: false);

        await recovery.TryRecoverAsync(Ct);
        await recovery.TryRecoverAsync(Ct);
        _time.Advance(ReachabilityRecovery.MinInterval);
        await recovery.TryRecoverAsync(Ct);

        _probes.Should().Be(2);
        _tracker.Current.State.Should().Be(ServerState.Offline);
    }

    [Fact]
    public async Task ProbeThatThrows_DoesNotEscape()
    {
        _tracker.ReportCallFailed();
        var recovery = new ReachabilityRecovery(_tracker, _ => throw new HttpRequestException("no route"), _time);

        var act = () => recovery.TryRecoverAsync(Ct);

        await act.Should().NotThrowAsync();
    }
}
