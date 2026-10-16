import { Collapsible } from '@base-ui/react/collapsible';
import { createContext, use, useState, type ComponentProps, type ReactNode } from 'react';
import { cn } from '../../../ui/cn';
import { Icon } from '../../../ui/Icon';

/**
 * A lesson's interview questions (spec 004 §3.5): the question is visible, the model answer is folded until tapped.
 * Nothing is scored or saved (PRD R21); the folds reset when the lesson is reopened.
 */
interface QaListState {
  isOpen: (index: number) => boolean;
  toggle: (index: number, open: boolean) => void;
}

const QaListContext = createContext<QaListState>({ isOpen: () => true, toggle: () => {} });
const QaItemContext = createContext(0);

const chevron = ['m6 9 6 6 6-6'];

export function InterviewQuestionList({ count, children }: { count: number; children: ReactNode }) {
  const [open, setOpen] = useState<ReadonlySet<number>>(new Set());
  const allOpen = open.size === count;

  const toggle = (index: number, next: boolean) =>
    setOpen((before) => {
      const after = new Set(before);
      if (next) after.add(index);
      else after.delete(index);
      return after;
    });
  const toggleAll = () => setOpen(allOpen ? new Set() : new Set(Array.from({ length: count }, (_, i) => i)));

  return (
    <div className="qa-list">
      <p className="qa-head">
        <span className="tabular-nums">
          {count} question{count === 1 ? '' : 's'} · answer in your head, then reveal
        </span>
        <button type="button" className="qa-all" onClick={toggleAll}>
          {allOpen ? 'Hide all' : 'Show all'}
        </button>
      </p>
      <QaListContext value={{ isOpen: (index) => open.has(index), toggle }}>{children}</QaListContext>
    </div>
  );
}

/** One question and its answer; the heading keeps its id, so links to a question still work. */
export function InterviewQuestion({ index, children }: { index: number; children: ReactNode }) {
  const list = use(QaListContext);
  return (
    <Collapsible.Root open={list.isOpen(index)} onOpenChange={(next) => list.toggle(index, next)}>
      <QaItemContext value={index}>
        <section className="qa-item">{children}</section>
      </QaItemContext>
    </Collapsible.Root>
  );
}

/** The `###` heading, holding the button that reveals the answer (disclosure pattern). */
export function InterviewQuestionHeading({ children, ...props }: ComponentProps<'h3'>) {
  const index = use(QaItemContext);
  return (
    <h3 {...props} className="qa-question">
      <Collapsible.Trigger className="qa-trigger">
        <span aria-hidden="true" className="qa-number">
          Q{index + 1}
        </span>
        <span className="min-w-0 flex-1">{children}</span>
        <span className={cn('qa-chevron')} aria-hidden="true">
          <Icon d={chevron} size={18} />
        </span>
      </Collapsible.Trigger>
    </h3>
  );
}

export function InterviewAnswer({ children }: { children: ReactNode }) {
  return (
    <Collapsible.Panel className="qa-answer">
      <div className="qa-answer-body">{children}</div>
    </Collapsible.Panel>
  );
}
