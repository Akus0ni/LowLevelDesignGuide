import { Link, useParams } from 'react-router';
import { useDataSource } from '../../data/DataSourceContext';
import type { LessonDetail, LessonLink, ModuleDetail } from '../../data/types';
import { content, preloadContent, useLesson, useModule } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { BackLink } from './BackLink';
import { ContentBoundary } from './ContentBoundary';
import { MarkdownView } from './markdown/MarkdownView';
import { chevronLeft, chevronRight, usePreload } from './navigation';
import { OutlineColumn, OutlineSheet } from './Outline';
import { LessonSkeleton } from './Skeletons';
import { useAnchorScroll } from './useAnchorScroll';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];

/** The reader: a lesson's text, its outline, its cheat sheet one tap away, and the next lesson. */
export function LessonPage() {
  const { module = '', lesson = '' } = useParams();
  return (
    <div className="pt-4 md:pt-8">
      <ContentBoundary fallback={<LessonSkeleton />}>
        <LessonView moduleSlug={module} lessonSlug={lesson} />
      </ContentBoundary>
    </div>
  );
}

function LessonView({ moduleSlug, lessonSlug }: { moduleSlug: string; lessonSlug: string }) {
  const module = useModule(moduleSlug);
  const position = module.lessons.findIndex((l) => l.slug === lessonSlug);
  // An unknown slug asks for an id that can't exist, so the boundary shows "not found" like any missing lesson.
  const lesson = useLesson(module.lessons[position]?.id ?? `missing:${lessonSlug}`);
  const source = useDataSource();
  useAnchorScroll();

  // The next lesson is the likeliest tap: have it ready.
  if (lesson.next) {
    const next = content.lesson(lesson.next.id);
    preloadContent(source, next.key, next.fetch);
  }

  return (
    <div className="lg:grid lg:grid-cols-[minmax(0,1fr)_13rem] lg:gap-14">
      <title>{`${lesson.title} · LLD Guide`}</title>
      <article className="min-w-0">
        <BackLink to={`/learn/${module.slug}`}>{module.title}</BackLink>
        <LessonHeader lesson={lesson} module={module} position={position + 1} />
        <MarkdownView markdown={lesson.bodyMarkdown} terms={lesson.terms} />
        <PreviousNext previous={lesson.previous} next={lesson.next} />
      </article>
      <aside className="hidden lg:block">
        <OutlineColumn outline={lesson.outline} />
      </aside>
    </div>
  );
}

function LessonHeader({
  lesson,
  module,
  position,
}: {
  lesson: LessonDetail;
  module: ModuleDetail;
  position: number;
}) {
  return (
    <header className="mt-3 border-b border-rule pb-6">
      <p className="font-mono text-[12px] tracking-[0.08em] text-muted tabular-nums">
        lesson {String(position).padStart(2, '0')} of {String(module.lessons.length).padStart(2, '0')} ·{' '}
        {lesson.readingMinutes} min read
      </p>
      <h1 className="mt-1.5 text-[32px] font-semibold md:text-[44px]">{lesson.title}</h1>
      <p className="mt-3 max-w-prose text-[17px] leading-relaxed text-muted">{lesson.summary}</p>
      <div className="mt-5 flex flex-wrap items-center gap-2">
        <Link
          to={`/revise/${lesson.moduleSlug}/${lesson.slug}`}
          className={cn(
            'inline-flex h-10 items-center gap-2 rounded-full bg-accent px-4 text-[14px] font-medium text-white select-none',
            'transition-[transform,opacity] duration-[120ms] ease-out active:scale-[0.97] hover:opacity-90',
            'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
          )}
        >
          <Icon d={boltIcon} size={16} />
          Cheat sheet
        </Link>
        <div className="lg:hidden">
          <OutlineSheet outline={lesson.outline} />
        </div>
      </div>
    </header>
  );
}

function PreviousNext({ previous, next }: { previous: LessonLink | null; next: LessonLink | null }) {
  if (!previous && !next) return null;
  return (
    <nav
      aria-label="More in this module"
      className="mt-16 grid gap-3 border-t border-rule pt-8 sm:grid-cols-2"
    >
      {previous && <NeighbourCard link={previous} direction="previous" />}
      {next && <NeighbourCard link={next} direction="next" />}
    </nav>
  );
}

function NeighbourCard({ link, direction }: { link: LessonLink; direction: 'previous' | 'next' }) {
  const preload = usePreload(content.lesson(link.id));
  const isNext = direction === 'next';
  return (
    <Link
      to={`/learn/${link.moduleSlug}/${link.slug}`}
      {...preload}
      className={cn(
        'flex items-center gap-3 rounded-lg border border-rule bg-surface p-4 select-none',
        'transition-[transform,background-color] duration-[120ms] ease-out active:scale-[0.98] hover:bg-surface/70',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        isNext ? 'sm:col-start-2 sm:flex-row-reverse sm:text-right' : '',
      )}
    >
      <span className="shrink-0 text-muted">
        <Icon d={isNext ? chevronRight : chevronLeft} size={20} />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block font-mono text-[12px] tracking-[0.08em] text-muted">{direction}</span>
        <span className="mt-0.5 block leading-snug font-semibold">{link.title}</span>
      </span>
    </Link>
  );
}
