import { Popover } from '@base-ui/react/popover';
import { useState } from 'react';
import { Link } from 'react-router';
import type { LessonTerm } from '../../../data/types';

/**
 * A type name in code that the lesson explains (its entities table). Tapping it opens a small card with the type's
 * role and "Read more", which jumps to the table. The card scales from the word it belongs to.
 */
export function TermCallout({ term, children }: { term: LessonTerm; children: string }) {
  const [open, setOpen] = useState(false);
  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger className="term-callout" aria-label={`${term.term}: what is it?`}>
        {children}
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Positioner side="top" sideOffset={6} collisionPadding={12} className="z-40">
          <Popover.Popup className="term-card">
            <Popover.Title className="font-mono text-[13px] font-semibold text-fg">{term.term}</Popover.Title>
            <Popover.Description className="mt-1 text-[14px] leading-snug text-muted">
              {term.role}
            </Popover.Description>
            {term.anchor && (
              <Link
                to={{ search: `?at=${encodeURIComponent(term.anchor)}` }}
                onClick={() => setOpen(false)}
                className="mt-2 inline-flex h-8 items-center text-[13px] font-medium text-accent select-none focus-visible:outline-2 focus-visible:outline-accent"
              >
                Read more →
              </Link>
            )}
          </Popover.Popup>
        </Popover.Positioner>
      </Popover.Portal>
    </Popover.Root>
  );
}
