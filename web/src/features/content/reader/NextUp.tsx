import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import type { LessonDetail } from '../../../data/types';
import { content } from '../../../data/useContent';
import { cn } from '../../../ui/cn';
import { Icon } from '../../../ui/Icon';
import { chevronLeft, chevronRight, usePreload } from '../navigation';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];

/**
 * The end of a lesson: what to do next. The next lesson in the module, or, on the last one, its cheat sheet. The card
 * rises into view the first time it's reached (it marks "you finished this lesson"); a plain link goes back.
 */
export function NextUp({ lesson }: { lesson: LessonDetail }) {
  const card = useRef<HTMLAnchorElement>(null);
  const [arrived, setArrived] = useState(false);
  const preload = usePreload(content.lesson(lesson.next?.id ?? lesson.id));

  useEffect(() => {
    const element = card.current;
    if (!element || typeof IntersectionObserver === 'undefined') {
      setArrived(true);
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          setArrived(true);
          observer.disconnect();
        }
      },
      { threshold: 0.4 },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [lesson.id]);

  const next = lesson.next;
  const to = next ? `/learn/${next.moduleSlug}/${next.slug}` : `/revise/${lesson.moduleSlug}/${lesson.slug}`;

  return (
    <nav aria-label="Next up" className="mt-16 border-t border-rule pt-8">
      <Link
        ref={card}
        to={to}
        {...(next ? preload : {})}
        className={cn(
          'flex items-center gap-4 rounded-lg border border-rule bg-surface p-5 select-none',
          'transition-[transform,opacity,background-color] duration-[320ms] ease-out active:scale-[0.98] hover:bg-surface/70',
          'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
          'motion-reduce:translate-y-0',
          arrived ? 'translate-y-0 opacity-100' : 'translate-y-2 opacity-0',
        )}
      >
        <span className="min-w-0 flex-1">
          <span className="block font-mono text-[12px] tracking-[0.08em] text-accent">
            {next ? 'next up' : 'revise it'}
          </span>
          <span className="mt-1 block text-[19px] leading-snug font-semibold">
            {next ? next.title : lesson.cheatSheetTitle}
          </span>
        </span>
        <span className="shrink-0 text-accent">
          <Icon d={next ? chevronRight : boltIcon} size={22} />
        </span>
      </Link>
      {lesson.previous && (
        <Link
          to={`/learn/${lesson.previous.moduleSlug}/${lesson.previous.slug}`}
          className="mt-3 inline-flex h-11 items-center gap-1 text-[15px] text-muted select-none hover:text-fg focus-visible:outline-2 focus-visible:outline-accent"
        >
          <Icon d={chevronLeft} size={18} />
          {lesson.previous.title}
        </Link>
      )}
    </nav>
  );
}
