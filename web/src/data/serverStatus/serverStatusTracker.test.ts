import { describe, expect, it } from 'vitest';
import type { ServerStatus } from '../types';
import {
  MAX_CLOCK_SKEW_MS,
  STALE_AFTER_MS,
  ServerStatusTracker,
  type StatusMessage,
} from './serverStatusTracker';

// Mirrors tests/Lld.ServerStatus.Tests/ServerStatusTrackerTests.cs: the two trackers follow the same rules.
function setup(initial: 'unknown' | 'online' = 'unknown') {
  let now = Date.parse('2026-10-15T09:00:00Z');
  const tracker = new ServerStatusTracker(() => now, initial);
  const changes: ServerStatus[] = [];
  tracker.subscribe((s) => changes.push(s));
  return {
    tracker,
    changes,
    now: () => now,
    advance: (ms: number) => (now += ms),
  };
}

const at = (ms: number) => new Date(ms).toISOString();
const online = (ms: number, reason = 'startup'): StatusMessage => ({
  v: 1,
  status: 'online',
  reason,
  at: at(ms),
});
const offline = (ms: number, reason = 'shutdown'): StatusMessage => ({
  v: 1,
  status: 'offline',
  reason,
  at: at(ms),
});

describe('ServerStatusTracker', () => {
  it('starts unknown with no signal', () => {
    const { tracker } = setup();
    expect(tracker.current).toMatchObject({ state: 'unknown', reason: 'no_signal' });
  });

  it('applies a fresh message and notifies', () => {
    const { tracker, changes, now } = setup();
    expect(tracker.apply(online(now() - 60_000))).toBe(true);
    expect(tracker.current.state).toBe('online');
    expect(changes).toHaveLength(1);
  });

  it('ignores older or equal messages as replays, in any order', () => {
    const { tracker, now } = setup();
    tracker.apply(offline(now() - 5 * 60_000));
    expect(tracker.apply(online(now() - 30 * 60_000))).toBe(false);
    expect(tracker.apply(online(now() - 5 * 60_000))).toBe(false);
    expect(tracker.current.state).toBe('offline');
  });

  it('ignores messages from the far future', () => {
    const { tracker, now } = setup();
    expect(tracker.apply(online(now() + MAX_CLOCK_SKEW_MS + 1000))).toBe(false);
    expect(tracker.apply(online(now() + MAX_CLOCK_SKEW_MS))).toBe(true);
  });

  it('does not let a genuine but stale message describe the present', () => {
    const { tracker, changes, now } = setup();
    expect(tracker.apply(online(now() - STALE_AFTER_MS - 60_000))).toBe(true);
    expect(tracker.current.state).toBe('unknown');
    expect(changes).toHaveLength(0);
  });

  it('treats a heartbeat with the same state as no news', () => {
    const { tracker, changes, now } = setup();
    tracker.apply(online(now() - 6 * 3600_000));
    tracker.apply(online(now(), 'heartbeat'));
    expect(changes).toHaveLength(1);
    expect(tracker.current.reason).toBe('heartbeat');
  });

  it('goes offline once when calls fail, and online when a call succeeds', () => {
    const { tracker, changes, now } = setup();
    tracker.apply(online(now()));
    tracker.reportCallFailed();
    tracker.reportCallFailed();
    expect(tracker.current).toMatchObject({ state: 'offline', reason: 'unreachable' });
    tracker.reportCallSucceeded();
    expect(tracker.current).toMatchObject({ state: 'online', reason: 'reachable' });
    expect(changes).toHaveLength(3);
  });

  it('keeps a quota stop even when a call succeeds', () => {
    const { tracker, now } = setup();
    tracker.apply({ ...offline(now(), 'quota'), quotaResetsAt: at(now() + 10 * 86_400_000) });
    tracker.reportCallSucceeded();
    expect(tracker.current).toMatchObject({ state: 'offline', reason: 'quota' });
  });

  it('becomes unknown after too long without evidence', () => {
    const { tracker, now, advance } = setup();
    tracker.apply(online(now()));
    advance(STALE_AFTER_MS);
    tracker.evaluate();
    expect(tracker.current.state).toBe('online');
    advance(1000);
    tracker.evaluate();
    expect(tracker.current).toMatchObject({ state: 'unknown', reason: 'stale' });
  });

  it('when assumed online, stays online until a call fails', () => {
    const { tracker, advance } = setup('online');
    advance(86_400_000);
    tracker.evaluate();
    expect(tracker.current).toMatchObject({ state: 'online', reason: 'assumed' });
    tracker.reportCallFailed();
    expect(tracker.current.state).toBe('offline');
  });
});
