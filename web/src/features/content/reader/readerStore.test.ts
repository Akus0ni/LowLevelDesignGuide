import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  getTextSize,
  loadPosition,
  loadReadSections,
  resetReaderStoreForTests,
  savePosition,
  saveReadSections,
  setTextSize,
  subscribeToTextSize,
} from './readerStore';

afterEach(() => {
  window.localStorage.clear();
  resetReaderStoreForTests();
  vi.restoreAllMocks();
});

describe('reader store', () => {
  it('defaults to medium text and remembers a choice across restarts', () => {
    expect(getTextSize()).toBe('m');
    const listener = vi.fn();
    subscribeToTextSize(listener);

    setTextSize('l');
    resetReaderStoreForTests(); // a new app run reads storage again

    expect(getTextSize()).toBe('l');
    expect(listener).toHaveBeenCalledOnce();
  });

  it('ignores an unknown stored size', () => {
    window.localStorage.setItem('lld.reader.textSize', JSON.stringify('xxl'));

    expect(getTextSize()).toBe('m');
  });

  it('keeps reading position and read sections per lesson', () => {
    savePosition('a', { anchor: 'overview', offset: 120 });
    saveReadSections('a', new Set(['overview', 'details']));

    expect(loadPosition('a')).toEqual({ anchor: 'overview', offset: 120 });
    expect(loadPosition('b')).toBeNull();
    expect(loadReadSections('a')).toEqual(new Set(['overview', 'details']));
    expect(loadReadSections('b')).toEqual(new Set());
  });

  it('keeps working when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('denied');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota');
    });

    expect(() => savePosition('a', { anchor: '', offset: 0 })).not.toThrow();
    expect(loadPosition('a')).toBeNull();
    expect(getTextSize()).toBe('m');
  });
});
