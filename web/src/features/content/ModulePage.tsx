import { Link, useParams } from 'react-router';
import type { LessonSummary } from '../../data/types';
import { content, useModule } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { ModuleCount, ModuleHatch } from '../progress/ModuleProgress';
import { countDone, sectionsRead, statusOf } from '../progress/progressMath';
import { StatusMark } from '../progress/StatusMark';
import { useProgress } from '../progress/useProgress';
import { ContentBoundary } from './ContentBoundary';
import { LessonIcon } from './LessonIcon';
import { ModuleMap } from './ModuleMap';
import { ModuleMnemonics } from './ModuleMnemonics';
import { BackLink } from './BackLink';
import { chevronRight, formatMinutes, plural, usePreload } from './navigation';
import { ModuleSkeleton } from './Skeletons';

/** One module, drawn as a single class box whose members are its lessons, numbered in reading order. */
export function ModulePage() {
  const { module = '' } = useParams();
  return (
    <section className="pt-4 md:pt-8">
      <BackLink to="/learn">Learn</BackLink>
      <ContentBoundary fallback={<ModuleSkeleton />}>
        <ModuleView slug={module} />
      </ContentBoundary>
    </section>
  );
}

function ModuleView({ slug }: { slug: string }) {
  const module = useModule(slug);
  const { byId } = useProgress();
  const count = countDone(byId, module.lessons);
  const minutes = module.lessons.reduce((total, lesson) => total + lesson.readingMinutes, 0);

  return (
    <>
      <title>{`${module.title} · LLD Guide`}</title>
      <p className="mt-3 font-mono text-[12px] tracking-[0.08em] text-muted">«module»</p>
      <h1 className="mt-1 text-[34px] font-semibold md:text-5xl">{module.title}</h1>
      <p className="mt-2 max-w-prose text-muted">{module.summary}</p>
      <div className="-mx-5 mt-8 bg-drafting px-5 py-6 md:-mx-8 md:rounded-3xl md:px-8 md:py-8">
        {module.mnemonics && module.mnemonics.length > 0 && (
          <ModuleMnemonics mnemonics={module.mnemonics} lessons={module.lessons} />
        )}
        {module.map && module.lessons.length > 0 && <ModuleMap map={module.map} lessons={module.lessons} />}
        {module.lessons.length === 0 ? (
          <p className="rounded-lg border border-dashed border-rule bg-surface px-4 py-8 text-center text-muted">
            Lessons for this module are being written.
          </p>
        ) : (
          <section
            aria-label="Lessons"
            data-inked={count.done === count.total || undefined}
            className="module-frame overflow-hidden rounded-lg border bg-surface"
          >
            <p className="relative flex items-baseline justify-between gap-3 border-b border-rule px-4 py-2.5 font-mono text-[12px] text-muted">
              <ModuleHatch count={count} />
              <span className="relative">lessons</span>
              <span className="relative tabular-nums">
                <ModuleCount count={count} /> done · {plural(module.lessons.length, 'lesson')} · {formatMinutes(minutes)}
              </span>
            </p>
            <ol>
              {module.lessons.map((lesson, index) => (
                <li key={lesson.id} className="border-t border-rule first:border-t-0">
                  <LessonRow lesson={lesson} position={index + 1} />
                </li>
              ))}
            </ol>
          </section>
        )}
      </div>
    </>
  );
}

function LessonRow({ lesson, position }: { lesson: LessonSummary; position: number }) {
  const preload = usePreload(content.lesson(lesson.id));
  const { byId } = useProgress();
  const status = statusOf(byId, lesson.id);
  return (
    <Link
      to={`/learn/${lesson.moduleSlug}/${lesson.slug}`}
      {...preload}
      className={cn(
        'flex items-start gap-3 px-4 py-3.5 select-none',
        // Rows highlight on press rather than scale, like native grouped lists.
        'transition-colors duration-150 ease-out active:bg-fg/[0.05] hover:bg-fg/[0.025]',
        'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-accent',
      )}
    >
      {/* Reading order is real information here, so the number stays. */}
      <span aria-hidden="true" className="mt-0.5 w-6 shrink-0 font-mono text-[13px] text-accent tabular-nums">
        {String(position).padStart(2, '0')}
      </span>
      <LessonIcon icon={lesson.icon} className="mt-px text-[20px] leading-none" />
      <span className="min-w-0 flex-1">
        <span className={cn('block leading-snug font-semibold wrap-anywhere', status === 'notStarted' && 'text-fg/80')}>
          {lesson.title}
        </span>
        <span className="mt-0.5 line-clamp-2 text-[15px] leading-relaxed text-muted">{lesson.summary}</span>
        {status === 'inProgress' && lesson.sectionCount > 0 && (
          <span className="mt-1 block font-mono text-[12px] text-accent tabular-nums">
            {sectionsRead(byId, lesson)} of {lesson.sectionCount} sections read
          </span>
        )}
      </span>
      <span className="mt-0.5 flex shrink-0 items-center gap-2 font-mono text-[12px] text-muted tabular-nums">
        {formatMinutes(lesson.readingMinutes)}
        <StatusMark status={status} />
        <Icon d={chevronRight} size={18} />
      </span>
    </Link>
  );
}
