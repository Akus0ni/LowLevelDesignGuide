import type { ReadingPosition } from '../../data/types';

type KeyValueStore = Pick<Storage, 'length' | 'key' | 'getItem' | 'removeItem'>;

const READ_PREFIX = 'lld.reader.read.';
const POSITION_PREFIX = 'lld.reader.position.';

export interface LegacyReading {
  readSections: string[];
  position: ReadingPosition | null;
}

/**
 * Takes the section ticks and reading positions spec 003 kept per device in localStorage, and removes them: from now
 * on they live in the progress store, per user (spec 005 A12). Runs on every load but finds nothing after the first.
 */
export function takeLegacyReading(store: KeyValueStore | null = safeLocalStorage()): Map<string, LegacyReading> {
  const found = new Map<string, LegacyReading>();
  if (!store) return found;

  const keys = Array.from({ length: store.length }, (_, index) => store.key(index)).filter(
    (key): key is string => key !== null && (key.startsWith(READ_PREFIX) || key.startsWith(POSITION_PREFIX)),
  );
  for (const key of keys) {
    const isRead = key.startsWith(READ_PREFIX);
    const lessonId = key.slice((isRead ? READ_PREFIX : POSITION_PREFIX).length);
    const entry = found.get(lessonId) ?? { readSections: [], position: null };
    const value = parse(store.getItem(key));
    if (isRead && Array.isArray(value)) entry.readSections = value.filter((anchor) => typeof anchor === 'string');
    if (!isRead && isPosition(value)) entry.position = value;
    found.set(lessonId, entry);
    store.removeItem(key);
  }
  return found;
}

function parse(raw: string | null): unknown {
  try {
    return raw === null ? null : JSON.parse(raw);
  } catch {
    return null;
  }
}

function isPosition(value: unknown): value is ReadingPosition {
  const position = value as ReadingPosition | null;
  return typeof position?.anchor === 'string' && typeof position.offset === 'number';
}

function safeLocalStorage(): KeyValueStore | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}
