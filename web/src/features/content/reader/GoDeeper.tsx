import { Collapsible } from '@base-ui/react/collapsible';
import type { ReactNode } from 'react';
import { Icon } from '../../../ui/Icon';
import { useSearchableFolds } from './searchFolds';

const chevron = ['m6 9 6 6 6-6'];

/**
 * The optional half of a lesson (spec 004 §3.2): folded until the reader asks for it. The closed fold lists its topics,
 * so it is clear what is inside before opening it. Not remembered: every visit starts with the short card.
 */
export function GoDeeper({ topics, children }: { topics: string[]; children: ReactNode }) {
  const searchable = useSearchableFolds();
  return (
    <Collapsible.Root className="go-deeper">
      <Collapsible.Trigger className="go-deeper-trigger">
        <span className="min-w-0 flex-1">
          <span className="go-deeper-label">Open the deep dive</span>
          {topics.length > 0 && (
            <span className="go-deeper-topics" data-search-skip="">
              {topics.join(' · ')}
            </span>
          )}
        </span>
        <span className="go-deeper-chevron" aria-hidden="true">
          <Icon d={chevron} size={20} />
        </span>
      </Collapsible.Trigger>
      <Collapsible.Panel className="go-deeper-panel" hiddenUntilFound={searchable}>
        <div className="go-deeper-body">{children}</div>
      </Collapsible.Panel>
    </Collapsible.Root>
  );
}
