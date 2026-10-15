import { useSyncExternalStore } from 'react';
import { getTextSize, subscribeToTextSize, type TextSize } from './readerStore';

/** The reader's text size on this device; the control and the text update together. */
export function useTextSize(): TextSize {
  return useSyncExternalStore(subscribeToTextSize, getTextSize, () => 'm');
}
