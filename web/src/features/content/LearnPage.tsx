import { useEffect, useState, type CSSProperties } from 'react';
import { Link } from 'react-router';
import type { ModuleDetail, ModuleSummary } from '../../data/types';
import { content, useModuleDetails, useModules } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { ContinueCard } from '../progress/ContinueCard';
import { ModuleCount, ModuleHatch } from '../progress/ModuleProgress';
import { countDone, statusOf } from '../progress/progressMath';
import { StatusMark } from '../progress/StatusMark';
import { useProgress } from '../progress/useProgress';
import { LessonIcon } from './LessonIcon';
import { ContentBoundary } from './ContentBoundary';
import { chevronRight, formatMinutes, usePreload } from './navigation';
import { DiagramSkeleton } from './Skeletons';

/** Lessons listed inside a module box before it says "+ N more". */
const MAX_MEMBERS = 4;
/** Delay between one box (or connector) and the next in the intro. */
const STEP_MS = 70;

/** The intro plays once per app run: Learn is opened many times a day, and the second time should be instant. */
let introPlayed = false;

/** Learn: the curriculum drawn as a class diagram, modules linked in the order they build on each other. */
export function LearnPage() {
  return (
    <section className="pt-8 md:pt-12">
      <title>Learn · LLD Guide</title>
      <h1 className="text-[34px] font-semibold md:text-5xl">Learn</h1>
      <p className="mt-2 max-w-prose text-muted">
        OOP, SOLID, design patterns, UML and case studies, in the order they build on each other.
      </p>
      <ContentBoundary fallback={<DiagramSkeleton />}>
        <CurriculumDiagram />
      </ContentBoundary>
    </section>
  );
}

function CurriculumDiagram() {
  const modules = useModules();
  // Every module's lessons, asked for at once, so the boxes don't load one after another.
  const details = useModuleDetails(modules.map((module) => module.slug));
  const [intro] = useState(() => !introPlayed);
  useEffect(() => {
    introPlayed = true;
  }, []);

  const delay = (step: number): CSSProperties | undefined =>
    intro ? { animationDelay: `${step * STEP_MS}ms` } : undefined;

  return (
    <>
      <ContinueCard modules={details} />
      <div className="-mx-5 mt-8 bg-drafting px-5 py-6 md:-mx-8 md:rounded-3xl md:px-8 md:py-8">
        <ol aria-label="Modules in reading order" className="max-w-xl">
          {modules.map((module, index) => (
            <li key={module.slug}>
              {index > 0 && <Connector intro={intro} style={delay(index * 2 - 1)} />}
              <ModuleBox module={module} detail={details[index]} intro={intro} style={delay(index * 2)} />
            </li>
          ))}
        </ol>
      </div>
    </>
  );
}

/** A UML dependency arrow (dashed line, open head): "read this next". */
function Connector({ intro, style }: { intro: boolean; style?: CSSProperties }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 12 32"
      width="12"
      height="32"
      style={style}
      className={cn(
        'mx-auto block text-muted/70',
        intro && 'animate-draw motion-reduce:animate-[appear_200ms_ease-out_both]',
      )}
    >
      <line x1="6" y1="0" x2="6" y2="29" stroke="currentColor" strokeWidth="1.5" strokeDasharray="3 3" />
      <path
        d="M1.5 23.5 6 30.5l4.5-7"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ModuleBox({
  module,
  detail,
  intro,
  style,
}: {
  module: ModuleSummary;
  detail: ModuleDetail;
  intro: boolean;
  style?: CSSProperties;
}) {
  const { byId } = useProgress();
  const count = countDone(byId, detail.lessons);
  const preload = usePreload(content.module(module.slug));
  const members = detail.lessons.slice(0, MAX_MEMBERS);
  const more = detail.lessons.length - members.length;
  const headingId = `module-${module.slug}`;

  return (
    <article
      aria-labelledby={headingId}
      style={style}
      className={cn(
        'module-frame overflow-hidden rounded-lg border bg-surface',
        intro && 'animate-rise motion-reduce:animate-[appear_200ms_ease-out_both]',
      )}
      data-inked={(count.total > 0 && count.done === count.total) || undefined}
    >
      <Link
        to={`/learn/${module.slug}`}
        {...preload}
        className={cn(
          'relative flex items-start gap-3 px-4 pt-3 pb-3.5 select-none',
          'transition-colors duration-150 ease-out active:bg-fg/[0.05] hover:bg-fg/[0.025]',
          'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-accent',
        )}
      >
        <ModuleHatch count={count} />
        <span className="relative min-w-0 flex-1">
          <span className="flex items-baseline justify-between gap-3 font-mono text-[11px] tracking-[0.08em] text-muted">
            «module»
            {count.total > 0 && <ModuleCount count={count} />}
          </span>
          <h2 id={headingId} className="mt-0.5 text-[22px] leading-tight font-semibold">
            {module.title}
          </h2>
        </span>
        <span className="relative mt-4 shrink-0 text-muted">
          <Icon d={chevronRight} size={20} />
        </span>
      </Link>
      <p className="border-t border-rule px-4 py-3 text-[15px] leading-relaxed text-muted">
        {module.summary}
      </p>
      <ul className="border-t border-rule py-1.5">
        {members.map((lesson) => (
          <li key={lesson.id}>
            <MemberLink to={`/learn/${lesson.moduleSlug}/${lesson.slug}`} preload={content.lesson(lesson.id)}>
              {lesson.icon ? (
                <LessonIcon icon={lesson.icon} className="w-[1ch] text-[15px]" />
              ) : (
                <span aria-hidden="true" className="font-mono text-accent">
                  +
                </span>
              )}
              <span
                className={cn(
                  'min-w-0 flex-1 truncate',
                  statusOf(byId, lesson.id) === 'notStarted' && 'text-muted',
                )}
              >
                {lesson.title}
              </span>
              <span className="font-mono text-[12px] text-muted tabular-nums">
                {formatMinutes(lesson.readingMinutes)}
              </span>
              <StatusMark status={statusOf(byId, lesson.id)} />
            </MemberLink>
          </li>
        ))}
        {more > 0 && (
          <li>
            <MemberLink to={`/learn/${module.slug}`} preload={content.module(module.slug)}>
              <span className="font-mono text-[13px] text-muted">+ {more} more</span>
            </MemberLink>
          </li>
        )}
        {members.length === 0 && (
          <li className="px-4 py-2 font-mono text-[13px] text-muted italic">no lessons yet</li>
        )}
      </ul>
    </article>
  );
}

function MemberLink({
  to,
  preload,
  children,
}: {
  to: string;
  preload: Parameters<typeof usePreload>[0];
  children: React.ReactNode;
}) {
  const handlers = usePreload(preload);
  return (
    <Link
      to={to}
      {...handlers}
      className={cn(
        'flex min-h-10 items-center gap-2.5 px-4 py-2 text-[15px] select-none',
        'transition-colors duration-150 ease-out active:bg-fg/[0.05] hover:bg-fg/[0.025]',
        'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-accent',
      )}
    >
      {children}
    </Link>
  );
}
