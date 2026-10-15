import { useState } from 'react';
import type { ServerStatus } from '../../data/types';
import { cn } from '../../ui/cn';
import { isBannerVisible, useServerStatus } from './ServerStatusContext';

const dateFormat = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' });

function messageFor(status: ServerStatus): { title: string; detail: string } {
  const detail = 'Your work stays on this device.';
  if (status.state === 'offline' && status.reason === 'quota') {
    const until = status.quotaResetsAt ? ` until ${dateFormat.format(new Date(status.quotaResetsAt))}` : '';
    return { title: `Server resting${until}`, detail };
  }
  if (status.state === 'unknown') return { title: 'Server status unknown', detail };
  return { title: 'Server offline', detail };
}

/**
 * A pill that overlays content instead of pushing it down (no layout shift).
 * Phone: just above the tab bar, sliding out from behind it (clear of the page heading and of the
 * top-center toasts). Tablet/desktop: top of the content column.
 * Motion (emil-design-eng): rare state change, so a standard transition — transform + opacity only,
 * strong ease-out, enter 200 ms, exit faster (150 ms); reduced motion keeps only the fade.
 */
export function ServerOfflineBanner() {
  const { status } = useServerStatus();
  const visible = isBannerVisible(status);

  // Keep the last message while the pill slides away, so the exit never shows an empty pill.
  const current = messageFor(status);
  const [shown, setShown] = useState(current);
  if (visible && shown.title !== current.title) setShown(current);

  return (
    <div
      className={cn(
        // z-[5]: below the tab bar (z-10), so the pill slides out from behind it.
        'pointer-events-none fixed inset-x-0 z-[5] flex justify-center px-4',
        'bottom-[calc(3.5rem+env(safe-area-inset-bottom,0px)+0.5rem)]',
        'md:top-0 md:bottom-auto md:z-20 md:pt-safe md:pl-64',
      )}
    >
      <div
        role="status"
        aria-live="polite"
        aria-hidden={!visible}
        data-visible={visible}
        className={cn(
          'flex max-w-full items-center gap-2 rounded-full bg-chrome px-4 py-2 text-sm shadow-sm backdrop-blur-xl md:mt-2',
          'ring-1 ring-black/5 dark:ring-white/10',
          'transition-[transform,opacity] duration-200 ease-out data-[visible=false]:duration-150',
          'data-[visible=false]:translate-y-[calc(100%+0.5rem)] data-[visible=false]:opacity-0',
          'md:data-[visible=false]:-translate-y-[calc(100%+0.5rem)]',
          'motion-reduce:data-[visible=false]:translate-y-0 md:motion-reduce:data-[visible=false]:translate-y-0',
        )}
      >
        <span aria-hidden className="size-2 shrink-0 rounded-full bg-amber-500" />
        <span className="min-w-0 truncate">
          <strong className="font-semibold">{shown.title}</strong>
          <span className="text-muted"> · {shown.detail}</span>
        </span>
      </div>
    </div>
  );
}
