/**
 * The Android Back button, as the native app asks for it (`window.lldBack()` from MainPage). Back first dismisses
 * whatever is open on top (the outline sheet, the diagram viewer, a popover), then search marks, then walks the app's history, and only
 * on the first screen lets the app leave.
 */
export type BackResult = 'closed' | 'back' | 'first';

/** Base UI marks an open sheet, dialog or popover with `data-open`; portals put the topmost one last. */
const OPEN_OVERLAY = '[role="dialog"][data-open]';
const MATCH_BAR = '[data-match-bar]';

export function handleBack(): BackResult {
  const overlays = document.querySelectorAll<HTMLElement>(OPEN_OVERLAY);
  const topmost = overlays[overlays.length - 1];
  if (topmost) {
    // Escape is how every Base UI overlay already dismisses itself (focus returns to its trigger).
    topmost.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    return 'closed';
  }

  // Search marks on a lesson or cheat sheet (spec 009 §3.6) clear before Back leaves the page.
  const matchBar = document.querySelector<HTMLElement>(MATCH_BAR);
  if (matchBar) {
    matchBar.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    return 'closed';
  }

  // react-router keeps its position in history.state.idx; 0 is the first screen of this session.
  const index = (window.history.state as { idx?: number } | null)?.idx ?? 0;
  if (index > 0) {
    window.history.back();
    return 'back';
  }
  return 'first';
}

declare global {
  interface Window {
    lldBack?: () => BackResult;
  }
}

export function installBackHandler(): void {
  window.lldBack = handleBack;
}
