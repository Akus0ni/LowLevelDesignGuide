import { cn } from '../../ui/cn';

/**
 * A concept's face (spec 004 §3.8): the same emoji on the map, the lists, the lesson and its cheat sheet, so the
 * concept is recognised before its name is read. Decorative: the title next to it carries the meaning.
 */
export function LessonIcon({ icon, className }: { icon?: string | null; className?: string }) {
  if (!icon) return null;
  return (
    <span aria-hidden="true" className={cn('lesson-icon', className)}>
      {icon}
    </span>
  );
}
