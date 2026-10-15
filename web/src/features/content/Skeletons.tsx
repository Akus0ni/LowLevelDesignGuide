import type { ReactNode } from 'react';
import { cn } from '../../ui/cn';

/** Screen readers hear "Loading…" at once; the outlines only fade in if loading takes over 200ms. */
function Loading({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div className={className}>
      <span className="sr-only">Loading…</span>
      <div aria-hidden="true" className="animate-appear-late">
        {children}
      </div>
    </div>
  );
}

/** An empty class box: the same outline the content will fill, so nothing jumps when it arrives. */
const box = 'rounded-lg border border-rule bg-surface';
const bar = 'rounded bg-fg/[0.06]';

export function DiagramSkeleton() {
  return (
    <Loading className="-mx-5 mt-8 bg-drafting px-5 py-6 md:-mx-8 md:rounded-3xl md:px-8 md:py-8">
      <div className="flex max-w-xl flex-col gap-8">
        {[0, 1].map((i) => (
          <div key={i} className={cn(box, 'p-4')}>
            <div className={cn(bar, 'h-3 w-16')} />
            <div className={cn(bar, 'mt-2 h-6 w-1/2')} />
            <div className={cn(bar, 'mt-6 h-4 w-full')} />
            <div className={cn(bar, 'mt-2 h-4 w-3/4')} />
          </div>
        ))}
      </div>
    </Loading>
  );
}

export function ModuleSkeleton() {
  return (
    <Loading className="mt-3">
      <div className={cn(bar, 'h-3 w-16')} />
      <div className={cn(bar, 'mt-3 h-9 w-2/3')} />
      <div className={cn(bar, 'mt-3 h-5 w-full max-w-md')} />
      <div className={cn(box, 'mt-8 h-56')} />
    </Loading>
  );
}
