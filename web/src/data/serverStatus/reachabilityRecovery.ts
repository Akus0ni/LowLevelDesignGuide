import { LOCAL_REASONS, type ServerStatusTracker } from './serverStatusTracker';

export const RECOVERY_MIN_INTERVAL_MS = 10_000;

/**
 * Browser port of Lld.ServerStatus.ReachabilityRecovery. "Unreachable" is this app's own conclusion (one of its
 * calls failed); if only the device lost its connection, the server never sends a new "online". So re-check
 * with one cheap call when the connection may be back (browser `online` event, status stream reopened).
 * Never a timer. The probe reports its own outcome to the tracker.
 */
export class ReachabilityRecovery {
  private lastProbeAt: number | null = null;
  private probing = false;
  private readonly tracker: ServerStatusTracker;
  private readonly probe: () => Promise<unknown>;
  private readonly now: () => number;

  constructor(tracker: ServerStatusTracker, probe: () => Promise<unknown>, now: () => number = Date.now) {
    this.tracker = tracker;
    this.probe = probe;
    this.now = now;
  }

  /** Only an app-inferred outage is re-checked; a signed "offline" (shutdown, quota) is the server's word. */
  get isNeeded(): boolean {
    const { state, reason } = this.tracker.current;
    return state === 'offline' && reason === LOCAL_REASONS.unreachable;
  }

  /** Resolves true when a probe was made. */
  async tryRecover(): Promise<boolean> {
    const now = this.now();
    if (!this.isNeeded || this.probing) return false;
    if (this.lastProbeAt !== null && now - this.lastProbeAt < RECOVERY_MIN_INTERVAL_MS) return false;

    this.probing = true;
    this.lastProbeAt = now;
    try {
      await this.probe();
    } catch {
      // Still unreachable: the probe already reported the failure.
    } finally {
      this.probing = false;
    }
    return true;
  }
}
