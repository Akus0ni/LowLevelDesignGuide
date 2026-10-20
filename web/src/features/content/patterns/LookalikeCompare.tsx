import { useId, useLayoutEffect, useRef, useState, type CSSProperties, type KeyboardEvent, type RefObject } from 'react';
import { Link, useParams } from 'react-router';
import { useDataSource } from '../../../data/DataSourceContext';
import type { LessonSummary, Lookalike } from '../../../data/types';
import { content, preloadContent, useLesson, useModule } from '../../../data/useContent';
import { BackLink } from '../BackLink';
import { ContentBoundary } from '../ContentBoundary';
import { LessonIcon } from '../LessonIcon';
import { MarkdownView } from '../markdown/MarkdownView';
import { LessonSkeleton } from '../Skeletons';
import { findLookalike } from './lookalikes';
import { patternFacts } from './patternFacts';

/** A pane needs about this much room to show its idea and structure diagram readably. */
const MIN_PANE_PX = 300;

/** The look-alike compare (spec 006 §3.7): route `learn/:module/compare/:pair`. */
export function LookalikeCompare() {
  const { module = '', pair = '' } = useParams();
  return (
    <div className="pt-4 md:pt-8">
      <ContentBoundary fallback={<LessonSkeleton />}>
        <CompareView moduleSlug={module} pair={pair} />
      </ContentBoundary>
    </div>
  );
}

function CompareView({ moduleSlug, pair }: { moduleSlug: string; pair: string }) {
  const module = useModule(moduleSlug);
  const lookalike = findLookalike(module.lookalikes, pair);
  const lessons = (lookalike?.lessons ?? [])
    .map((slug) => module.lessons.find((lesson) => lesson.slug === slug))
    .filter((lesson): lesson is LessonSummary => lesson !== undefined);

  // Ask for every lesson at once; each pane then reads its own from the cache.
  const source = useDataSource();
  for (const lesson of lessons) {
    const { key, fetch } = content.lesson(lesson.id);
    preloadContent(source, key, fetch);
  }

  if (!lookalike || lessons.length < 2) {
    return (
      <>
        <BackLink to={`/learn/${moduleSlug}`}>{module.title}</BackLink>
        <p className="mt-6 text-muted">This comparison doesn't exist. Pick one from the module page.</p>
      </>
    );
  }

  return <Compare moduleSlug={moduleSlug} moduleTitle={module.title} lookalike={lookalike} lessons={lessons} />;
}

/**
 * Patterns that look alike, side by side: the one difference and the question that tells them apart on top, then each
 * pattern's tile, hook, idea and structure. Columns when they fit; otherwise a switch, with every pane kept in the same
 * grid cell so switching never moves the page.
 */
function Compare({
  moduleSlug,
  moduleTitle,
  lookalike,
  lessons,
}: {
  moduleSlug: string;
  moduleTitle: string;
  lookalike: Lookalike;
  lessons: LessonSummary[];
}) {
  const root = useRef<HTMLDivElement>(null);
  const split = useFits(root, lessons.length * MIN_PANE_PX);
  const [shown, setShown] = useState(0);
  const id = useId();
  const names = lessons.map((lesson) => lesson.title).join(' vs ');

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    const next = (shown + (event.key === 'ArrowRight' ? 1 : lessons.length - 1)) % lessons.length;
    setShown(next);
    document.getElementById(`${id}-tab-${next}`)?.focus();
  };

  return (
    <article ref={root} className="lookalike" data-layout={split ? 'split' : 'tabs'}>
      <title>{`${names} · LLD Guide`}</title>
      <BackLink to={`/learn/${moduleSlug}`}>{moduleTitle}</BackLink>
      <p className="mt-3 font-mono text-[12px] tracking-[0.08em] text-muted">«look-alikes»</p>
      <h1 className="mt-1 text-[28px] leading-tight font-semibold md:text-4xl">{names}</h1>
      <aside className="lookalike-stamp" aria-label="The difference">
        <p className="lookalike-stamp-label">the one difference</p>
        <p className="lookalike-difference">{lookalike.difference}</p>
        <p className="lookalike-ask">
          <span>Ask yourself:</span> {lookalike.ask}
        </p>
      </aside>
      {!split && (
        <div role="tablist" aria-label="Show" className="lookalike-switch" onKeyDown={onKeyDown}>
          {lessons.map((lesson, index) => (
            <button
              key={lesson.slug}
              id={`${id}-tab-${index}`}
              type="button"
              role="tab"
              aria-selected={shown === index}
              aria-controls={`${id}-pane-${index}`}
              tabIndex={shown === index ? 0 : -1}
              onClick={() => setShown(index)}
            >
              {lesson.symbol ?? lesson.title}
              <span className="sr-only"> {lesson.title}</span>
            </button>
          ))}
        </div>
      )}
      <div className="lookalike-panes" style={{ '--panes': lessons.length } as CSSProperties}>
        {lessons.map((lesson, index) => {
          const visible = split || shown === index;
          return (
            <section
              key={lesson.slug}
              id={`${id}-pane-${index}`}
              role={split ? undefined : 'tabpanel'}
              aria-labelledby={split ? undefined : `${id}-tab-${index}`}
              aria-label={split ? lesson.title : undefined}
              className="lookalike-pane"
              data-shown={visible || undefined}
              inert={!visible}
              aria-hidden={!visible || undefined}
            >
              <Pane lesson={lesson} />
            </section>
          );
        })}
      </div>
    </article>
  );
}

function Pane({ lesson }: { lesson: LessonSummary }) {
  const detail = useLesson(lesson.id);
  const facts = patternFacts(detail.bodyMarkdown);
  const markdown = [facts.idea, facts.structure ? `\`\`\`mermaid\n${facts.structure}\n\`\`\`` : ''].filter(Boolean).join('\n\n');
  return (
    <>
      <div className="lookalike-pane-head">
        <span aria-hidden="true" className="picker-symbol">
          {lesson.symbol}
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-[17px] leading-snug font-semibold">{lesson.title}</span>
          {lesson.hook && <span className="block text-[14px] text-muted">{lesson.hook}</span>}
        </span>
        <LessonIcon icon={lesson.icon} className="text-[22px]" />
      </div>
      <MarkdownView markdown={markdown} textSize="s" />
      <Link to={`/learn/${lesson.moduleSlug}/${lesson.slug}`} className="ptable-compare mt-2">
        Open {lesson.title} →
      </Link>
    </>
  );
}

/** True when the element is at least `needed` pixels wide; checked again whenever it is resized. */
function useFits(element: RefObject<HTMLElement | null>, needed: number): boolean {
  const [fits, setFits] = useState(false);
  useLayoutEffect(() => {
    const node = element.current;
    if (!node || typeof ResizeObserver === 'undefined') return;
    const check = () => setFits(node.clientWidth >= needed);
    check();
    const observer = new ResizeObserver(check);
    observer.observe(node);
    return () => observer.disconnect();
  }, [element, needed]);
  return fits;
}
