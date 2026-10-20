import { useId, useRef, useState, type KeyboardEvent } from 'react';
import { cn } from '../../../ui/cn';
import { CodeLines } from './CodeBlock';
import type { Fence, FenceSide } from './fence';
import { longestLine, useFitsSideBySide } from './fitSideBySide';

const SIDES: FenceSide[] = ['before', 'after'];

/**
 * A design that breaks a principle next to the fixed one (spec 004 §3.4). Lines the refactor removed are marked − in
 * Before, lines it added or changed are marked + in After (worked out when content was packed).
 *
 * Side by side when both panes fit their longest line; otherwise one box with a Before / After switch. Both panes stay
 * rendered in the same grid cell, so the box keeps the taller pane's height and switching never moves the page.
 */
export function CodeCompare({ before, after }: { before: Fence; after: Fence }) {
  const figure = useRef<HTMLElement>(null);
  const split = useFitsSideBySide(figure, Math.max(longestLine(before.code), longestLine(after.code)));
  const [side, setSide] = useState<FenceSide>('before');
  const id = useId();
  const fences = { before, after };
  const language = before.language;

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    const next = event.key === 'ArrowLeft' ? 'before' : 'after';
    setSide(next);
    document.getElementById(`${id}-tab-${next}`)?.focus();
  };

  return (
    <figure
      ref={figure}
      className="code-block compare"
      data-layout={split ? 'split' : 'tabs'}
      aria-label={`Before and after${fences[side].meta.title ? `: ${fences[side].meta.title}` : ''}`}
    >
      <figcaption className="code-block-label compare-head">
        <span className="code-block-title" title={split ? undefined : fences[side].meta.title}>
          {split ? 'refactor' : (fences[side].meta.title ?? side)}
        </span>
        {split ? (
          language && <span>{language}</span>
        ) : (
          <span role="tablist" aria-label="Show" className="compare-switch" data-side={side} onKeyDown={onKeyDown}>
            {SIDES.map((name) => (
              <button
                key={name}
                id={`${id}-tab-${name}`}
                type="button"
                role="tab"
                aria-selected={side === name}
                aria-controls={`${id}-${name}`}
                tabIndex={side === name ? 0 : -1}
                onClick={() => setSide(name)}
              >
                {name}
              </button>
            ))}
          </span>
        )}
      </figcaption>
      <div className="compare-panes">
        {SIDES.map((name) => {
          const fence = fences[name];
          const shown = split || side === name;
          return (
            <section
              key={name}
              id={`${id}-${name}`}
              role={split ? undefined : 'tabpanel'}
              aria-labelledby={split ? undefined : `${id}-tab-${name}`}
              className={cn('compare-pane', `compare-pane-${name}`)}
              data-shown={shown || undefined}
              inert={!shown}
              aria-hidden={!shown || undefined}
            >
              {split && (
                <p className="compare-pane-label">
                  <span>{name}</span>
                  {fence.meta.title && (
                    <span className="code-block-title" title={fence.meta.title}>
                      {fence.meta.title}
                    </span>
                  )}
                </p>
              )}
              <CodeLines
                code={fence.code}
                info={fence.language}
                marks={fence.meta.marks}
                markKind={name === 'before' ? 'del' : 'add'}
                label={`${name}${fence.meta.title ? `: ${fence.meta.title}` : ''}`}
              />
            </section>
          );
        })}
      </div>
      <p className="sr-only">Lines marked minus were removed by the refactor; lines marked plus were added or changed.</p>
    </figure>
  );
}
