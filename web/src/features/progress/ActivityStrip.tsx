import { cn } from '../../ui/cn';
import { activityWeek } from './progressMath';

const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

/**
 * This week at a glance (spec 005 §3.6): seven small boxes, inked on days with reading or revising. It only ever
 * counts up, so a missed day costs nothing (no streak to lose, PRD §3 "no gamification").
 */
export function ActivityStrip({ activityDays, today = new Date() }: { activityDays: readonly string[]; today?: Date }) {
  const week = activityWeek(activityDays, today);
  const active = week.filter((day) => day.active).length;

  return (
    <div>
      <ol aria-label="This week" className="flex gap-1.5">
        {week.map((day, index) => (
          <li
            key={day.day}
            aria-label={`${DAY_NAMES[index]}: ${day.active ? 'studied' : 'no study'}${day.isToday ? ' (today)' : ''}`}
            className={cn(
              'grid size-8 place-items-center rounded-md border font-mono text-[12px] select-none',
              day.active ? 'border-fg bg-fg text-bg' : 'border-rule bg-surface text-muted',
              day.isToday && !day.active && 'border-fg/60 text-fg',
            )}
          >
            <span aria-hidden="true">{day.label}</span>
          </li>
        ))}
      </ol>
      <p className="mt-2 font-mono text-[12px] text-muted tabular-nums">
        {active === 1 ? '1 day this week' : `${active} days this week`}
      </p>
    </div>
  );
}
