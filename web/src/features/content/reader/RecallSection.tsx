import { Collapsible } from '@base-ui/react/collapsible';
import { use, type ReactNode } from 'react';
import { cn } from '../../../ui/cn';
import { RecallContext } from './recall';
import { useSearchableFolds } from './searchFolds';

/**
 * One cheat sheet section. In recall mode its body folds under the heading behind a "reveal" button; revealing opens
 * it like an accordion (the one place height animates; reduced motion opens it at once).
 */
export function RecallSection({ id, heading, body }: { id: string; heading: ReactNode; body: ReactNode }) {
  const recall = use(RecallContext);
  const open = !recall.on || recall.isRevealed(id);
  const searchable = useSearchableFolds();

  return (
    <section data-recall={id}>
      {heading}
      <Collapsible.Root open={open} onOpenChange={(next) => next && recall.reveal(id)}>
        {!open && (
          <Collapsible.Trigger
            className={cn(
              'mt-2 flex h-12 w-full items-center justify-between rounded-lg border border-dashed border-rule px-4 text-left select-none',
              'font-mono text-[12px] tracking-[0.06em] text-muted',
              'transition-[transform,background-color,border-color] duration-[120ms] ease-out active:scale-[0.99] hover:border-accent hover:text-fg',
              'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
            )}
          >
            <span>think first, then reveal</span>
            <span className="text-accent">reveal</span>
          </Collapsible.Trigger>
        )}
        <Collapsible.Panel className="recall-panel" hiddenUntilFound={searchable}>
          {body}
        </Collapsible.Panel>
      </Collapsible.Root>
    </section>
  );
}
