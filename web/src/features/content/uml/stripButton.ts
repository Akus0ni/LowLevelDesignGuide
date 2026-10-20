import { cn } from '../../../ui/cn';

/** The small text buttons of a diagram's top strip (same look as the sequence step controls). */
export const stripButton = cn(
  'inline-flex h-8 min-w-8 items-center justify-center gap-1 rounded-md px-2 font-mono text-[11px] tracking-[0.06em] text-muted select-none',
  'transition-[transform,color,background-color] duration-[120ms] ease-out active:scale-[0.94] hover:text-fg',
  'focus-visible:outline-2 focus-visible:outline-accent disabled:pointer-events-none disabled:opacity-40',
);
