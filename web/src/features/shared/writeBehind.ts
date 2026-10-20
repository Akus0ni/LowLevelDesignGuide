/** Quiet time before queued changes are written: scrolling saves a position, and typing a note, many times a minute. */
export const FLUSH_DELAY_MS = 1000;

/**
 * When a store writes what it has queued (spec 005 plan §2.6, spec 007 plan §2.5): after a quiet second, when the page
 * is hidden, on `pagehide`, and when the store is disposed (the owner changes). Writes run one after another.
 */
export class WriteBehind {
  private timer: ReturnType<typeof setTimeout> | undefined;
  private chain: Promise<void> = Promise.resolve();
  private readonly write: () => Promise<void>;
  private readonly events?: EventTarget;

  constructor(write: () => Promise<void>, events?: EventTarget) {
    this.write = write;
    this.events = events;
    this.events?.addEventListener('visibilitychange', this.flushWhenHidden);
    this.events?.addEventListener('pagehide', this.flushNow);
  }

  /** Something was queued: write it once things are quiet. */
  schedule(): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(this.flushNow, FLUSH_DELAY_MS);
  }

  /** Writes everything queued. Safe to call any time; calls run one after another. */
  flush(): Promise<void> {
    this.chain = this.chain.then(() => this.write());
    return this.chain;
  }

  /** Stop listening and write what is left. */
  dispose(): Promise<void> {
    clearTimeout(this.timer);
    this.events?.removeEventListener('visibilitychange', this.flushWhenHidden);
    this.events?.removeEventListener('pagehide', this.flushNow);
    return this.flush();
  }

  private readonly flushNow = () => void this.flush();

  private readonly flushWhenHidden = () => {
    if (document.visibilityState === 'hidden') void this.flush();
  };
}
