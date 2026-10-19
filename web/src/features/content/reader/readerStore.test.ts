import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  getTextSize,
  loadRecall,
  resetReaderStoreForTests,
  saveRecall,
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

  it('remembers recall mode', () => {
    expect(loadRecall()).toBe(false);

    saveRecall(true);

    expect(loadRecall()).toBe(true);
  });

  it('keeps working when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('denied');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota');
    });

    expect(() => saveRecall(true)).not.toThrow();
    expect(loadRecall()).toBe(false);
    expect(getTextSize()).toBe('m');
  });
});
