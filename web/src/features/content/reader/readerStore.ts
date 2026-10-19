/**
 * Reader preferences, kept on this device only (spec 003 T14). Reading state (ticks, position) moved to the per-user
 * progress store in spec 005. Storage can be missing or full (private mode, WebView settings), so every access is
 * guarded and the reader still works without it.
 */

export type TextSize = 's' | 'm' | 'l';

const PREFIX = 'lld.reader.';
const TEXT_SIZE_KEY = `${PREFIX}textSize`;
const TEXT_SIZES: readonly TextSize[] = ['s', 'm', 'l'];

function read<T>(key: string): T | null {
  try {
    const raw = window.localStorage.getItem(key);
    return raw === null ? null : (JSON.parse(raw) as T);
  } catch {
    return null;
  }
}

function write(key: string, value: unknown): void {
  try {
    window.localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Not saved; the reader keeps working for this session.
  }
}

// ---- Text size: an external store, so the control and the text update together ---------------------------------

const listeners = new Set<() => void>();
let textSize: TextSize | null = null;

export function getTextSize(): TextSize {
  if (textSize === null) {
    const stored = read<string>(TEXT_SIZE_KEY);
    textSize = TEXT_SIZES.includes(stored as TextSize) ? (stored as TextSize) : 'm';
  }
  return textSize;
}

export function setTextSize(size: TextSize): void {
  textSize = size;
  write(TEXT_SIZE_KEY, size);
  listeners.forEach((listener) => listener());
}

export function subscribeToTextSize(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

// ---- Cheat sheets -------------------------------------------------------------------------------------------------

export const loadRecall = (): boolean => read<boolean>(`${PREFIX}recall`) === true;

export const saveRecall = (on: boolean) => write(`${PREFIX}recall`, on);

/** Test seam: forget the cached text size so the next read comes from storage again. */
export function resetReaderStoreForTests(): void {
  textSize = null;
}
