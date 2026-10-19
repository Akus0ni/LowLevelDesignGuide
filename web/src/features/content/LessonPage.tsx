import { Link, useParams } from 'react-router';
import { useDataSource } from '../../data/DataSourceContext';
import type { LessonDetail, ModuleDetail } from '../../data/types';
import { content, preloadContent, useLesson, useModule } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { DoneControl } from '../progress/DoneControl';
import { Icon } from '../../ui/Icon';
import { BackLink } from './BackLink';
import { LessonIcon } from './LessonIcon';
import { ContentBoundary } from './ContentBoundary';
import { MarkdownView } from './markdown/MarkdownView';
import { formatMinutes } from './navigation';
import { OutlineColumn, OutlineSheet } from './Outline';
import { NextUp } from './reader/NextUp';
import { TextSizeControl } from './reader/TextSizeControl';
import { useReadingTracker } from './reader/useReadingTracker';
import { useTextSize } from './reader/useTextSize';
import { LessonSkeleton } from './Skeletons';
import { useAnchorScroll } from './useAnchorScroll';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];

/** The reader: a lesson's text, its outline, its cheat sheet one tap away, and what to read next. */
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
  const textSize = useTextSize();
  const reading = useReadingTracker(lesson.id, lesson.outline);
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
        <LessonHeader lesson={lesson} module={module} position={position + 1} reading={reading} />
        <MarkdownView markdown={lesson.bodyMarkdown} terms={lesson.terms} textSize={textSize} />
        <footer className="mt-16 border-t border-rule pt-8">
          <DoneControl lessonId={lesson.id} sectionCount={lesson.outline.length} />
          <NextUp lesson={lesson} />
        </footer>
      </article>
      <aside className="hidden lg:block">
        <OutlineColumn outline={lesson.outline} current={reading.current} read={reading.read} />
      </aside>
    </div>
  );
}

function LessonHeader({
  lesson,
  module,
  position,
  reading,
}: {
  lesson: LessonDetail;
  module: ModuleDetail;
  position: number;
  reading: ReturnType<typeof useReadingTracker>;
}) {
  return (
    <header className="mt-3 border-b border-rule pb-6">
      <p className="font-mono text-[12px] tracking-[0.08em] text-muted tabular-nums">
        lesson {String(position).padStart(2, '0')} of {String(module.lessons.length).padStart(2, '0')} ·{' '}
        {formatMinutes(lesson.readingMinutes)} read
      </p>
      <div className="mt-1.5 flex items-start gap-3 text-[32px] md:text-[44px]">
        <LessonIcon icon={lesson.icon} className="lesson-icon-tile mt-[0.05em]" />
        <h1 className="min-w-0 text-[1em] font-semibold">{lesson.title}</h1>
      </div>
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
          <OutlineSheet outline={lesson.outline} current={reading.current} read={reading.read} />
        </div>
        <TextSizeControl />
      </div>
    </header>
  );
}
