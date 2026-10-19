import type { LessonStatus } from '../../data/types';
import { cn } from '../../ui/cn';

const LABEL: Record<LessonStatus, string> = {
  notStarted: 'Not started',
  inProgress: 'In progress',
  done: 'Done',
};

/**
 * A lesson's status, the same everywhere (spec 005 §3.11): a pencil circle (not started), half inked in blueprint blue
 * (in progress), a solid ink disc with a check (done). The shape tells the status, not only the colour, and it has a
 * text label for screen readers.
 */
export function StatusMark({ status, className }: { status: LessonStatus; className?: string }) {
  return (
    <svg
      viewBox="0 0 16 16"
      width="16"
      height="16"
      role="img"
      aria-label={LABEL[status]}
      className={cn(
        'shrink-0',
        status === 'notStarted' && 'text-muted',
        status === 'inProgress' && 'text-accent',
        status === 'done' && 'text-fg',
        className,
      )}
    >
      {status === 'done' ? (
        <>
          <circle cx="8" cy="8" r="7" fill="currentColor" />
          <path d="m4.8 8.2 2.1 2.1 4.3-4.6" fill="none" stroke="var(--surface)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        </>
      ) : (
        <>
          <circle cx="8" cy="8" r="6.25" fill="none" stroke="currentColor" strokeWidth="1.5" />
          {status === 'inProgress' && <path d="M8 1.75a6.25 6.25 0 0 1 0 12.5Z" fill="currentColor" />}
        </>
      )}
    </svg>
  );
}
