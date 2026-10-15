/**
 * Reader preferences and per-lesson reading state, kept on this device only (spec 003 T14 / T14b). Real progress and
 * sync between devices are specs 005 and 010. Storage can be missing or full (private mode, WebView settings), so
 * every access is guarded and the reader still works without it.
 */

export type TextSize = 's' | 'm' | 'l';

export interface ReadingPosition {
  /** Nearest heading above the top of the screen ('' = above the first heading). */
  anchor: string;
  /** Pixels scrolled past that heading. */
  offset: number;
}

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

// ---- Per lesson ---------------------------------------------------------------------------------------------------

export const loadPosition = (lessonId: string) => read<ReadingPosition>(`${PREFIX}position.${lessonId}`);

export const savePosition = (lessonId: string, position: ReadingPosition) =>
  write(`${PREFIX}position.${lessonId}`, position);

export const loadReadSections = (lessonId: string) =>
  new Set(read<string[]>(`${PREFIX}read.${lessonId}`) ?? []);

export const saveReadSections = (lessonId: string, anchors: Set<string>) =>
  write(`${PREFIX}read.${lessonId}`, [...anchors]);

/** Test seam: forget the cached text size so the next read comes from storage again. */
export function resetReaderStoreForTests(): void {
  textSize = null;
}
