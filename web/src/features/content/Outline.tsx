import { Drawer } from '@base-ui/react/drawer';
import { useState } from 'react';
import { Link } from 'react-router';
import type { OutlineEntry } from '../../data/types';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';

const listIcon = ['M8 6h12', 'M8 12h12', 'M8 18h12', 'M4 6h.01', 'M4 12h.01', 'M4 18h.01'];

/** The lesson's h2 / h3 headings as in-page links (`?at=anchor`). */
function OutlineList({ outline, onNavigate }: { outline: OutlineEntry[]; onNavigate?: () => void }) {
  return (
    <ol className="flex flex-col">
      {outline.map((entry) => (
        <li key={entry.anchor}>
          <Link
            to={{ search: `?at=${encodeURIComponent(entry.anchor)}` }}
            onClick={onNavigate}
            className={cn(
              'block rounded-md py-1.5 pr-2 text-[14px] leading-snug text-muted select-none',
              'transition-colors duration-150 ease-out hover:text-fg active:text-fg',
              'focus-visible:outline-2 focus-visible:outline-accent',
              entry.level === 3 ? 'pl-5' : 'pl-2',
            )}
          >
            {entry.text}
          </Link>
        </li>
      ))}
    </ol>
  );
}

/** Desktop: a sticky column beside the text. */
export function OutlineColumn({ outline }: { outline: OutlineEntry[] }) {
  if (outline.length === 0) return null;
  return (
    <nav
      aria-label="On this page"
      className="sticky top-8 max-h-[calc(100dvh-4rem)] overflow-y-auto overscroll-contain"
    >
      <p className="mb-2 pl-2 font-mono text-[12px] tracking-[0.08em] text-muted">on this page</p>
      <OutlineList outline={outline} />
    </nav>
  );
}

/**
 * Phone: an "On this page" button opens the outline as a bottom sheet. Base UI's Drawer handles focus, Esc and
 * swipe-to-dismiss; the sheet follows the finger (--drawer-swipe-movement-y) and closes with the iOS drawer curve.
 */
export function OutlineSheet({ outline }: { outline: OutlineEntry[] }) {
  const [open, setOpen] = useState(false);
  if (outline.length === 0) return null;
  return (
    <Drawer.Root open={open} onOpenChange={setOpen} swipeDirection="down">
      <Drawer.Trigger
        className={cn(
          'inline-flex h-10 items-center gap-2 rounded-full border border-rule bg-surface px-3.5 text-[14px] font-medium select-none',
          'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
          'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        )}
      >
        <Icon d={listIcon} size={18} />
        On this page
      </Drawer.Trigger>
      <Drawer.Portal>
        <Drawer.Backdrop className="sheet-backdrop" />
        <Drawer.Viewport className="fixed inset-0 z-30 flex items-end justify-center">
          <Drawer.Popup className="sheet">
            <div aria-hidden="true" className="mx-auto mt-2.5 mb-1 h-1 w-9 rounded-full bg-fg/15" />
            <Drawer.Title className="px-5 pt-2 pb-1 font-mono text-[12px] tracking-[0.08em] text-muted">
              on this page
            </Drawer.Title>
            <Drawer.Content className="max-h-[60dvh] overflow-y-auto overscroll-contain px-3 pb-[calc(1rem+env(safe-area-inset-bottom,0px))]">
              {/* Choosing a heading closes the sheet, so the page can scroll to it. */}
              <OutlineList outline={outline} onNavigate={() => setOpen(false)} />
            </Drawer.Content>
          </Drawer.Popup>
        </Drawer.Viewport>
      </Drawer.Portal>
    </Drawer.Root>
  );
}
