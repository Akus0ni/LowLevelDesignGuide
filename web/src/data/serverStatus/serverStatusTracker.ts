import type { ServerStatus } from '../types';

/** The signed payload the PC publishes (mirrors Lld.ServerStatus.StatusMessage). */
export interface StatusMessage {
  v: number;
  status: 'online' | 'offline';
  reason: string;
  at: string;
  quotaResetsAt?: string;
}

export const MAX_CLOCK_SKEW_MS = 5 * 60_000;
export const STALE_AFTER_MS = 7 * 60 * 60_000;

/** Reasons set on the device, never published (same values as Lld.ServerStatus.StatusReasons). */
export const LOCAL_REASONS = {
  noSignal: 'no_signal',
  stale: 'stale',
  unreachable: 'unreachable',
  reachable: 'reachable',
  assumed: 'assumed',
} as const;

export const QUOTA_REASON = 'quota';

type Listener = (status: ServerStatus) => void;

export const isStoppedForQuota = (status: ServerStatus) =>
  status.state === 'offline' && status.reason === QUOTA_REASON;

/**
 * Browser port of Lld.ServerStatus.ServerStatusTracker; the same rules, checked by the same tests:
 * newer-than-last messages only (anti-replay), no far-future messages, failed calls mean offline,
 * successful calls mean online (unless stopped for quota), and old news becomes unknown.
 */
export class ServerStatusTracker {
  private status: ServerStatus;
  private lastMessageAt: number | null = null;
  private lastEvidenceAt: number | null = null;
  private readonly listeners = new Set<Listener>();
  private readonly now: () => number;

  constructor(now: () => number = Date.now, initial: 'unknown' | 'online' = 'unknown') {
    this.now = now;
    this.status = {
      state: initial,
      reason: initial === 'online' ? LOCAL_REASONS.assumed : LOCAL_REASONS.noSignal,
      since: new Date(now()).toISOString(),
    };
  }

  get current(): ServerStatus {
    return this.status;
  }

  subscribe(listener: Listener): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  /** Applies a verified message; false when ignored as a replay or from the far future. */
  apply(message: StatusMessage): boolean {
    const now = this.now();
    const at = Date.parse(message.at);
    if (Number.isNaN(at) || at > now + MAX_CLOCK_SKEW_MS) return false;
    if (this.lastMessageAt !== null && at <= this.lastMessageAt) return false;

    this.lastMessageAt = at;
    if (now - at > STALE_AFTER_MS) return true; // genuine, but too old to describe the present

    this.lastEvidenceAt = Math.max(this.lastEvidenceAt ?? at, at);
    const since = this.status.state === message.status ? this.status.since : message.at;
    this.transition({
      state: message.status,
      reason: message.reason,
      since,
      quotaResetsAt: message.quotaResetsAt,
    });
    return true;
  }

  reportCallFailed(): void {
    this.lastEvidenceAt = this.now();
    if (this.status.state !== 'offline') {
      this.transition({ state: 'offline', reason: LOCAL_REASONS.unreachable, since: this.nowIso() });
    }
  }

  reportCallSucceeded(): void {
    this.lastEvidenceAt = this.now();
    if (this.status.state !== 'online' && !isStoppedForQuota(this.status)) {
      this.transition({ state: 'online', reason: LOCAL_REASONS.reachable, since: this.nowIso() });
    }
  }

  /** Call periodically: with no recent evidence, the last known state can't be trusted. */
  evaluate(): void {
    const last = this.lastEvidenceAt;
    if (last !== null && this.now() - last > STALE_AFTER_MS && this.status.state !== 'unknown') {
      this.transition({ state: 'unknown', reason: LOCAL_REASONS.stale, since: this.nowIso() });
    }
  }

  private transition(next: ServerStatus): void {
    const previous = this.status;
    this.status = next;
    const isNews =
      previous.state !== next.state ||
      isStoppedForQuota(previous) !== isStoppedForQuota(next) ||
      previous.quotaResetsAt !== next.quotaResetsAt;
    if (isNews) this.listeners.forEach((listener) => listener(next));
  }

  private nowIso(): string {
    return new Date(this.now()).toISOString();
  }
}
