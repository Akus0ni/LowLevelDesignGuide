import { use } from 'react';
import { Link } from 'react-router';
import type { Lookalike, ModuleDetail } from '../../../data/types';
import { cn } from '../../../ui/cn';
import { LookalikeContext } from './lookalikeContext';
import { comparePath, lookalikesOf } from './lookalikes';

/**
 * "Often confused with" (spec 006 §3.7): after a pattern lesson's *Seen in .NET* section, one link per look-alike group
 * the lesson belongs to, opening the compare view. Renders nothing when the lesson has no look-alikes.
 */
export function LookalikeStrip() {
  const current = use(LookalikeContext);
  if (!current) return null;
  const groups = lookalikesOf(current.module.lookalikes, current.lessonSlug);
  return <LookalikeLinks module={current.module} groups={groups} label="often confused with" />;
}

/** The module page's "Look-alikes" list: every compare in the module, with its question. */
export function LookalikeList({ module }: { module: ModuleDetail }) {
  return <LookalikeLinks module={module} groups={module.lookalikes ?? []} label="look-alikes" className="lookalike-list" />;
}

function LookalikeLinks({
  module,
  groups,
  label,
  className,
}: {
  module: ModuleDetail;
  groups: Lookalike[];
  label: string;
  className?: string;
}) {
  if (groups.length === 0) return null;
  const titleOf = (slug: string) => module.lessons.find((lesson) => lesson.slug === slug)?.title ?? slug;
  return (
    <nav className={cn('lookalike-strip', className)} aria-label={label.charAt(0).toUpperCase() + label.slice(1)}>
      <p className="lookalike-strip-label">«{label}»</p>
      <ul>
        {groups.map((group) => (
          <li key={group.lessons.join()}>
            <Link to={comparePath(module.slug, group)} className="lookalike-strip-link">
              <span>{group.lessons.map(titleOf).join(' ⇄ ')}</span>
              <span className="lookalike-strip-ask">{group.ask}</span>
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}
