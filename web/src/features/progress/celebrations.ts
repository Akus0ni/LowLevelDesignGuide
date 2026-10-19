type KeyValueStore = Pick<Storage, 'getItem' | 'setItem'>;

const PREFIX = 'lld.celebrated.';

/**
 * What was already celebrated (a lesson or module done, spec 005 §3.8), so each moment plays once. A device preference
 * like text size: kept in localStorage per owner, not synced. Seeing a moment once more on a second device is harmless.
 */
export class Celebrations {
  private readonly key: string;
  private readonly store: KeyValueStore | null;
  private readonly seen: Set<string>;

  constructor(owner: string, store: KeyValueStore | null = safeLocalStorage()) {
    this.key = PREFIX + owner;
    this.store = store;
    this.seen = new Set(this.read());
  }

  has(id: string): boolean {
    return this.seen.has(id);
  }

  remember(id: string): void {
    this.seen.add(id);
    try {
      this.store?.setItem(this.key, JSON.stringify([...this.seen]));
    } catch {
      // Not kept: the moment may play once more next time.
    }
  }

  private read(): string[] {
    try {
      const value: unknown = JSON.parse(this.store?.getItem(this.key) ?? '[]');
      return Array.isArray(value) ? value.filter((id): id is string => typeof id === 'string') : [];
    } catch {
      return [];
    }
  }
}

export const moduleCelebrationId = (moduleSlug: string) => `module:${moduleSlug}`;

function safeLocalStorage(): KeyValueStore | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}
