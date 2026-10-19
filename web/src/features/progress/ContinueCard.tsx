import { Link } from 'react-router';
import type { ModuleDetail } from '../../data/types';
import { content } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { LessonIcon } from '../content/LessonIcon';
import { chevronRight, formatMinutes, usePreload } from '../content/navigation';
import { continueTarget, countDone, sectionsRead } from './progressMath';
import { useProgress } from './useProgress';

/**
 * The first thing on Learn (spec 005 §3.3): one tap back into the right lesson, at the line it was left. A small class
 * box with the amber "you are here" pin; the overall count rides along in mono. Gone once everything is done.
 */
export function ContinueCard({ modules }: { modules: readonly ModuleDetail[] }) {
  const { byId } = useProgress();
  const target = continueTarget(byId, modules);
  const preload = usePreload(content.lesson(target?.lesson.id ?? ''));
  if (!target) return null;

  const { lesson, kind } = target;
  const module = modules.find((m) => m.slug === lesson.moduleSlug);
  const overall = countDone(byId, modules.flatMap((m) => m.lessons));
  const read = sectionsRead(byId, lesson);

  return (
    <Link
      to={`/learn/${lesson.moduleSlug}/${lesson.slug}`}
      {...preload}
      aria-label={`${kind === 'continue' ? 'Continue' : 'Start'}: ${lesson.title}`}
      className={cn(
        'mt-6 block max-w-xl overflow-hidden rounded-lg border border-rule bg-surface select-none',
        'transition-[transform,background-color] duration-[120ms] ease-out active:scale-[0.98] hover:bg-surface/70',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
      )}
    >
      <span className="flex items-center justify-between gap-3 border-b border-rule px-4 py-2 font-mono text-[12px] tracking-[0.08em]">
        <span className="flex items-center gap-2 text-fg">
          {/* You are here: the one amber of the identity (spec 003 §3.7). */}
          <span aria-hidden="true" className="size-2 rounded-full bg-highlight" />
          {kind === 'continue' ? 'continue' : 'start'}
        </span>
        <span className="text-muted tabular-nums">
          {overall.done} of {overall.total} done
        </span>
      </span>
      <span className="flex items-center gap-3 px-4 py-3.5">
        <LessonIcon icon={lesson.icon} className="text-[26px] leading-none" />
        <span className="min-w-0 flex-1">
          {/* Clamped so a very long title can't turn the card into a screenful; the link's name has it in full. */}
          <span className="line-clamp-3 text-[19px] leading-snug font-semibold wrap-anywhere">{lesson.title}</span>
          <span className="mt-0.5 block truncate text-[14px] text-muted">{module?.title}</span>
          <span className="mt-1 block font-mono text-[12px] text-muted tabular-nums">
            {kind === 'continue' && lesson.sectionCount > 0
              ? `${read} of ${lesson.sectionCount} sections read`
              : formatMinutes(lesson.readingMinutes)}
          </span>
        </span>
        <span className="shrink-0 text-accent">
          <Icon d={chevronRight} size={22} />
        </span>
      </span>
    </Link>
  );
}
