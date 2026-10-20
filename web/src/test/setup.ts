import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// Without Vitest globals, Testing Library cannot register its auto-cleanup.
afterEach(cleanup);

// jsdom has no matchMedia, scrollIntoView or Element.scrollTo; components use them (colour scheme, reduced motion,
// anchors, the Code ↔ UML code pane).
if (!window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }) as MediaQueryList;
}
Element.prototype.scrollIntoView ??= function scrollIntoView() {};
Element.prototype.scrollTo ??= function scrollTo() {};
