import { describe, expect, it, vi } from 'vitest';
import { HttpDataSource } from '../httpDataSource';
import { AccessRequestOutbox } from './accessRequestOutbox';
import { RECOVERY_MIN_INTERVAL_MS, ReachabilityRecovery } from './reachabilityRecovery';
import { ServerStatusTracker } from './serverStatusTracker';

// Mirrors tests/Lld.ServerStatus.Tests/ReachabilityRecoveryTests.cs.
function setup(serverAnswers = true) {
  let now = Date.parse('2026-10-15T18:00:00Z');
  const tracker = new ServerStatusTracker(() => now, 'online');
  const probe = vi.fn(async () => {
    if (serverAnswers) tracker.reportCallSucceeded();
    else tracker.reportCallFailed();
  });
  const recovery = new ReachabilityRecovery(tracker, probe, () => now);
  return { tracker, probe, recovery, advance: (ms: number) => (now += ms) };
}

describe('ReachabilityRecovery', () => {
  it('re-checks once and goes online after the phone gets its connection back', async () => {
    const { tracker, probe, recovery } = setup();
    tracker.reportCallFailed();

    expect(await recovery.tryRecover()).toBe(true);

    expect(probe).toHaveBeenCalledTimes(1);
    expect(tracker.current.state).toBe('online');
  });

  it.each(['shutdown', 'quota', 'manual'])('does not probe a signed offline (%s)', async (reason) => {
    const { tracker, probe, recovery } = setup();
    tracker.apply({
      v: 1,
      status: 'offline',
      reason,
      at: new Date(Date.parse('2026-10-15T18:00:00Z')).toISOString(),
    });

    expect(await recovery.tryRecover()).toBe(false);
    expect(probe).not.toHaveBeenCalled();
  });

  it('probes at most once per interval while the connection flaps', async () => {
    const { tracker, probe, recovery, advance } = setup(false);
    tracker.reportCallFailed();

    await recovery.tryRecover();
    await recovery.tryRecover();
    advance(RECOVERY_MIN_INTERVAL_MS);
    await recovery.tryRecover();

    expect(probe).toHaveBeenCalledTimes(2);
  });
});

describe('HttpDataSource: mobile data off, request, data back on (owner report, spec 002)', () => {
  it('goes back online and sends the saved request without restarting the app', async () => {
    const fetchFn = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch')) // request access with data off
      .mockResolvedValueOnce(new Response('Healthy', { status: 200 })) // /health probe
      .mockResolvedValue(new Response(JSON.stringify({ message: 'ok' }), { status: 202 })); // outbox flush
    const events = new EventTarget();
    const store = new Map<string, string>();
    const outbox = new AccessRequestOutbox({
      getItem: (k) => store.get(k) ?? null,
      setItem: (k, v) => void store.set(k, v),
      removeItem: (k) => void store.delete(k),
    });
    const source = new HttpDataSource('', fetchFn as unknown as typeof fetch, {
      tracker: new ServerStatusTracker(Date.now, 'online'),
      outbox,
      events,
      connectStatus: () => () => {},
    });
    const sent = vi.fn();
    source.onAccessRequestSent(sent);
    source.onServerStatusChanged(() => {});

    expect(await source.requestAccess({ email: 'owner@example.com', name: 'Owner' })).toEqual({
      queued: true,
    });
    expect((await source.getServerStatus()).state).toBe('offline');

    events.dispatchEvent(new Event('online')); // data switched back on

    await vi.waitFor(() => expect(sent).toHaveBeenCalledWith('owner@example.com'));
    expect((await source.getServerStatus()).state).toBe('online');
    expect(fetchFn.mock.calls[1][0]).toBe('/health');
    expect(outbox.pending).toEqual([]);
  });
});
