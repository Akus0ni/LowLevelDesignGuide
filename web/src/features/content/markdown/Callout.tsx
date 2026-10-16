import type { ReactNode } from 'react';
import { Icon } from '../../../ui/Icon';
import type { CalloutType } from './rehypeContent';

/** Each callout reads as a UML note (folded corner) with a stereotype naming what kind of aside it is. */
const CALLOUTS: Record<CalloutType, { label: string; icon: string[] }> = {
  note: { label: 'note', icon: ['M12 16v-4', 'M12 8h.01', 'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z'] },
  tip: {
    label: 'tip',
    icon: ['M9 18h6', 'M10 22h4', 'M12 2a7 7 0 0 0-4 12.7c.6.5 1 1.3 1 2.3h6c0-1 .4-1.8 1-2.3A7 7 0 0 0 12 2Z'],
  },
  interview: {
    label: 'interview tip',
    icon: ['M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2Z', 'M8 9h8', 'M8 13h5'],
  },
  smell: { label: 'code smell', icon: ['M4 8c2-2 4 2 6 0s4-2 6 0 4 2 4 0', 'M4 14c2-2 4 2 6 0s4-2 6 0 4 2 4 0'] },
  pitfall: {
    label: 'pitfall',
    icon: ['M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z', 'M12 9v4', 'M12 17h.01'],
  },
  app: { label: 'in this app', icon: ['m16 18 6-6-6-6', 'm8 6-6 6 6 6'] },
  work: {
    label: 'from my work',
    icon: ['M16 20V4a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16', 'M4 6h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2Z'],
  },
};

export function Callout({ type, children }: { type: CalloutType; children: ReactNode }) {
  const { label, icon } = CALLOUTS[type];
  return (
    <aside className="callout" data-callout={type} aria-label={label}>
      <p className="callout-label">
        <Icon d={icon} size={14} />
        <span>«{label}»</span>
      </p>
      <div className="callout-body">{children}</div>
    </aside>
  );
}
