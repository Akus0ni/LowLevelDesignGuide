import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useCheatSheet, useModule } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { BackLink } from './BackLink';
import { LessonIcon } from './LessonIcon';
import { ContentBoundary } from './ContentBoundary';
import { MarkdownView } from './markdown/MarkdownView';
import { RecallContext } from './reader/recall';
import { loadRecall, saveRecall } from './reader/readerStore';
import { TextSizeControl } from './reader/TextSizeControl';
import { useTextSize } from './reader/useTextSize';
import { LessonSkeleton } from './Skeletons';

/** A cheat sheet: read it, or switch on recall to test yourself section by section. */
export function CheatSheetPage() {
  const { module = '', lesson = '' } = useParams();
  return (
    <div className="pt-4 md:pt-8">
      <ContentBoundary fallback={<LessonSkeleton />}>
        <CheatSheetView moduleSlug={module} lessonSlug={lesson} />
      </ContentBoundary>
    </div>
  );
}

function CheatSheetView({ moduleSlug, lessonSlug }: { moduleSlug: string; lessonSlug: string }) {
  const module = useModule(moduleSlug);
  const lessonId = module.lessons.find((l) => l.slug === lessonSlug)?.id ?? `missing:${lessonSlug}`;
  const sheet = useCheatSheet(lessonId);
  const textSize = useTextSize();
  const [recall, setRecall] = useState(loadRecall);
  const [revealed, setRevealed] = useState<Set<string>>(new Set());

  const toggleRecall = () => {
    setRecall(!recall);
    saveRecall(!recall);
    setRevealed(new Set());
  };
  const revealAll = () => setRevealed(new Set(sectionIds()));

  return (
    <article>
      <title>{`${sheet.title} · LLD Guide`}</title>
      <BackLink to="/revise">Revise</BackLink>
      <header className="mt-3 border-b border-rule pb-6">
        <p className="font-mono text-[12px] tracking-[0.08em] text-muted">
          cheat sheet · {sheet.moduleTitle}
        </p>
        <div className="mt-1.5 flex items-center gap-3 text-[32px] md:text-[44px]">
          <LessonIcon icon={sheet.icon} className="lesson-icon-tile" />
          <h1 className="min-w-0 text-[1em] font-semibold">{sheet.title}</h1>
        </div>
        <p className="mt-2 text-[15px] text-muted">
          from{' '}
          <Link
            to={`/learn/${sheet.moduleSlug}/${sheet.lessonSlug}`}
            className="font-medium text-accent underline decoration-accent/40 underline-offset-[3px] hover:decoration-accent"
          >
            {sheet.lessonTitle} →
          </Link>
        </p>
        <div className="mt-5 flex flex-wrap items-center gap-2">
          <RecallSwitch on={recall} onToggle={toggleRecall} />
          {recall && (
            <button type="button" onClick={revealAll} className={pill}>
              Reveal all
            </button>
          )}
          <TextSizeControl />
        </div>
      </header>
      <RecallContext
        value={{
          on: recall,
          isRevealed: (id) => revealed.has(id),
          reveal: (id) => setRevealed((before) => new Set(before).add(id)),
        }}
      >
        <MarkdownView markdown={sheet.bodyMarkdown} textSize={textSize} recall />
      </RecallContext>
    </article>
  );
}

/** The `##` sections recall mode folds on this page (their heading ids). */
function sectionIds(): string[] {
  return [...document.querySelectorAll<HTMLElement>('[data-recall]')]
    .map((section) => section.dataset.recall ?? '')
    .filter((id) => id.length > 0);
}

const pill = cn(
  'inline-flex h-10 items-center rounded-full border border-rule bg-surface px-4 text-[14px] font-medium select-none',
  'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
);

/** A switch, not a button: it has a state the reader can see ("Recall" on/off). */
function RecallSwitch({ on, onToggle }: { on: boolean; onToggle: () => void }) {
  return (
    <button type="button" role="switch" aria-checked={on} onClick={onToggle} className={cn(pill, 'gap-2.5')}>
      <span
        aria-hidden="true"
        className={cn(
          'relative h-5 w-8 rounded-full transition-colors duration-200 ease-out',
          on ? 'bg-accent' : 'bg-fg/15',
        )}
      >
        <span
          className={cn(
            'absolute top-0.5 left-0.5 size-4 rounded-full bg-white shadow-sm transition-transform duration-200 ease-out',
            on && 'translate-x-3',
          )}
        />
      </span>
      Recall
    </button>
  );
}
