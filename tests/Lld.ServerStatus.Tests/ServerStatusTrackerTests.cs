using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Lld.ServerStatus.Tests;

public sealed class ServerStatusTrackerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero));
    private readonly ServerStatusTracker _tracker;
    private readonly List<ServerStatusSnapshot> _changes = [];

    public ServerStatusTrackerTests()
    {
        _tracker = new ServerStatusTracker(_time);
        _tracker.Changed += (_, snapshot) => _changes.Add(snapshot);
    }

    private DateTimeOffset Now => _time.GetUtcNow();

    private static StatusMessage Online(DateTimeOffset at, string reason = StatusReasons.Startup) =>
        StatusMessage.Create(ServerState.Online, reason, at);

    private static StatusMessage Offline(DateTimeOffset at, string reason = StatusReasons.Shutdown) =>
        StatusMessage.Create(ServerState.Offline, reason, at);

    [Fact]
    public void StartsUnknown_WithNoSignal()
    {
        _tracker.Current.State.Should().Be(ServerState.Unknown);
        _tracker.Current.Reason.Should().Be(StatusReasons.NoSignal);
    }

    [Fact]
    public void Apply_FreshMessage_ChangesStateAndNotifies()
    {
        _tracker.Apply(Online(Now.AddMinutes(-1))).Should().BeTrue();

        _tracker.Current.State.Should().Be(ServerState.Online);
        _changes.Should().ContainSingle().Which.State.Should().Be(ServerState.Online);
    }

    [Fact]
    public void Apply_OlderOrEqualMessage_IsIgnoredAsReplay()
    {
        _tracker.Apply(Offline(Now.AddMinutes(-1)));

        _tracker.Apply(Online(Now.AddMinutes(-2))).Should().BeFalse();
        _tracker.Apply(Online(Now.AddMinutes(-1))).Should().BeFalse();

        _tracker.Current.State.Should().Be(ServerState.Offline);
    }

    [Fact]
    public void Apply_MessagesInAnyOrder_EndOnTheNewest()
    {
        _tracker.Apply(Offline(Now.AddMinutes(-5)));
        _tracker.Apply(Online(Now.AddMinutes(-30)));
        _tracker.Apply(Online(Now.AddMinutes(-60)));

        _tracker.Current.State.Should().Be(ServerState.Offline);
    }

    [Fact]
    public void Apply_MessageFromTheFarFuture_IsIgnored()
    {
        _tracker.Apply(Online(Now + ServerStatusTracker.MaxClockSkew + TimeSpan.FromSeconds(1))).Should().BeFalse();
        _tracker.Apply(Online(Now + ServerStatusTracker.MaxClockSkew)).Should().BeTrue();
    }

    [Fact]
    public void Apply_GenuineButStaleMessage_DoesNotDescribeThePresent()
    {
        _tracker.Apply(Online(Now - ServerStatusTracker.StaleAfter - TimeSpan.FromMinutes(1))).Should().BeTrue();

        _tracker.Current.State.Should().Be(ServerState.Unknown);
        _changes.Should().BeEmpty();
    }

    [Fact]
    public void Heartbeat_WithSameState_IsNotNews()
    {
        _tracker.Apply(Online(Now.AddHours(-6)));
        var since = _tracker.Current.Since;

        _tracker.Apply(Online(Now, StatusReasons.Heartbeat));

        _changes.Should().ContainSingle();
        _tracker.Current.Since.Should().Be(since);
        _tracker.Current.Reason.Should().Be(StatusReasons.Heartbeat);
    }

    [Fact]
    public void ReportCallFailed_GoesOffline_Once()
    {
        _tracker.Apply(Online(Now));

        _tracker.ReportCallFailed();
        _tracker.ReportCallFailed();

        _tracker.Current.State.Should().Be(ServerState.Offline);
        _tracker.Current.Reason.Should().Be(StatusReasons.Unreachable);
        _changes.Should().HaveCount(2);
    }

    [Fact]
    public void ReportCallSucceeded_GoesOnline()
    {
        _tracker.ReportCallFailed();

        _tracker.ReportCallSucceeded();

        _tracker.Current.State.Should().Be(ServerState.Online);
        _tracker.Current.Reason.Should().Be(StatusReasons.Reachable);
    }

    [Fact]
    public void ReportCallSucceeded_DoesNotLiftAQuotaStop()
    {
        var resetsAt = Now.AddDays(10);
        _tracker.Apply(StatusMessage.Create(ServerState.Offline, StatusReasons.Quota, Now, resetsAt));

        _tracker.ReportCallSucceeded();

        _tracker.Current.IsStoppedForQuota.Should().BeTrue();
        _tracker.Current.QuotaResetsAt.Should().Be(resetsAt);
    }

    [Fact]
    public void Evaluate_WithoutEvidenceForTooLong_BecomesUnknown()
    {
        _tracker.Apply(Online(Now));
        _time.Advance(ServerStatusTracker.StaleAfter);
        _tracker.Evaluate();
        _tracker.Current.State.Should().Be(ServerState.Online);

        _time.Advance(TimeSpan.FromSeconds(1));
        _tracker.Evaluate();

        _tracker.Current.State.Should().Be(ServerState.Unknown);
        _tracker.Current.Reason.Should().Be(StatusReasons.Stale);
    }

    [Fact]
    public void AssumedOnline_StaysOnlineUntilACallFails()
    {
        var tracker = new ServerStatusTracker(_time, ServerState.Online);
        _time.Advance(TimeSpan.FromDays(1));
        tracker.Evaluate();
        tracker.Current.Reason.Should().Be(StatusReasons.Assumed);

        tracker.ReportCallFailed();

        tracker.Current.State.Should().Be(ServerState.Offline);
    }

    [Fact]
    public void Observer_CanCallBackIntoTheTracker()
    {
        _tracker.Changed += (_, _) => _tracker.Evaluate();

        var act = _tracker.ReportCallFailed;

        act.Should().NotThrow();
        _tracker.Current.State.Should().Be(ServerState.Offline);
    }
}
