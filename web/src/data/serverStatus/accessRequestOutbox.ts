import type { AccessRequestInput } from '../types';

type KeyValueStore = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

const STORAGE_KEY = 'lld.accessRequestOutbox';

/**
 * Browser outbox for access requests made while the server is offline (Outbox pattern; the app's version is
 * C# AccessRequestOutbox). One entry per email: the latest details win, as on the server.
 */
export class AccessRequestOutbox {
  private readonly store: KeyValueStore | null;

  constructor(store: KeyValueStore | null = safeLocalStorage()) {
    this.store = store;
  }

  get pending(): AccessRequestInput[] {
    try {
      return JSON.parse(this.store?.getItem(STORAGE_KEY) ?? '[]') as AccessRequestInput[];
    } catch {
      return [];
    }
  }

  enqueue(request: AccessRequestInput): void {
    const email = request.email.trim().toLowerCase();
    this.save([...this.pending.filter((r) => r.email.trim().toLowerCase() !== email), request]);
  }

  /**
   * Sends saved requests in order. `send` resolves when delivered, returns false when the server is
   * unreachable (stop and keep the rest), and throws when the server refused it (drop it).
   */
  async flush(
    send: (request: AccessRequestInput) => Promise<boolean>,
    onSent: (r: AccessRequestInput) => void,
  ) {
    for (const request of this.pending) {
      let delivered: boolean;
      try {
        delivered = await send(request);
      } catch {
        this.remove(request); // e.g. invalid email: retrying can't help
        continue;
      }
      if (!delivered) return;
      this.remove(request);
      onSent(request);
    }
  }

  private remove(request: AccessRequestInput): void {
    this.save(this.pending.filter((r) => r.email !== request.email));
  }

  private save(requests: AccessRequestInput[]): void {
    if (requests.length === 0) this.store?.removeItem(STORAGE_KEY);
    else this.store?.setItem(STORAGE_KEY, JSON.stringify(requests));
  }
}

function safeLocalStorage(): KeyValueStore | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null; // storage disabled (private mode, blocked cookies)
  }
}
