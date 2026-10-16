import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { chevronLeft } from './navigation';

/** "‹ Learn": says where Back goes (wayfinding), with a 44px touch target. */
export function BackLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      title={typeof children === 'string' ? children : undefined}
      className={cn(
        '-ml-2 inline-flex h-11 max-w-full items-center gap-0.5 rounded-lg pr-3 pl-1 font-medium text-accent select-none',
        'transition-opacity duration-150 ease-out active:opacity-60',
        'focus-visible:outline-2 focus-visible:outline-accent',
      )}
    >
      <span className="shrink-0">
        <Icon d={chevronLeft} size={22} />
      </span>
      {/* Secondary navigation: one line, the full name in the tooltip and on the page it leads to. */}
      <span className="min-w-0 truncate">{children}</span>
    </Link>
  );
}
