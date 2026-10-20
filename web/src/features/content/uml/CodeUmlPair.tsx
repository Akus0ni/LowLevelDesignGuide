import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type MouseEvent,
} from 'react';
import { cn } from '../../../ui/cn';
import { CodeLines } from '../markdown/CodeBlock';
import { longestLine, useFitsSideBySide } from '../markdown/fitSideBySide';
import type { Fence } from '../markdown/fence';
import { MermaidDiagram } from '../markdown/MermaidDiagram';
import { linkAtLine, parseCodeLinks } from './codeLinks';

type Side = 'diagram' | 'code';
const SIDES: Side[] = ['diagram', 'code'];

/** Long enough to see what was picked before the phone switches to the other pane. */
const SWITCH_DELAY_MS = 300;
const NO_LINES: ReadonlySet<number> = new Set();

/**
 * Code ↔ UML (spec 008 §3.4): a diagram and the C# it stands for, checked against each other when content was packed.
 * Tap a box or an arrow and the code that makes it lights up; tap a lit-able code line and its box or arrow lights up.
 * Side by side when the code fits half the box; otherwise a Diagram / Code switch (the Before / After layout: both
 * panes share one grid cell, so switching never moves the page). Esc or "clear" in the diagram strip clears.
 */
export function CodeUmlPair({ diagram, code }: { diagram: Fence; code: Fence }) {
  const figure = useRef<HTMLElement>(null);
  const codePane = useRef<HTMLElement>(null);
  const split = useFitsSideBySide(figure, longestLine(code.code));
  const [side, setSide] = useState<Side>('diagram');
  const [selected, setSelected] = useState<string | null>(null);
  const links = useMemo(() => parseCodeLinks(diagram.meta.links), [diagram.meta.links]);
  const linkable = useMemo(() => new Set([...links.values()].flatMap((lines) => [...lines])), [links]);
  const marks = (selected && links.get(selected)) || NO_LINES;
  const switchTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const id = useId();

  useEffect(() => () => clearTimeout(switchTimer.current), []);

  // The first lit line comes into view inside the code pane (the page itself doesn't jump).
  useEffect(() => {
    if (!selected || (!split && side !== 'code')) return;
    codePane.current?.querySelector('.code-line[data-mark]')?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  }, [selected, split, side]);

  const showLater = (next: Side) => {
    clearTimeout(switchTimer.current);
    if (!split) switchTimer.current = setTimeout(() => setSide(next), SWITCH_DELAY_MS);
  };

  const selectFromDiagram = (key: string | null) => {
    setSelected(key);
    if (key && links.has(key)) showLater('code');
  };

  const onCodeClick = (event: MouseEvent<HTMLElement>) => {
    const line = event.target instanceof Element ? event.target.closest('.code-line') : null;
    if (!line || !codePane.current) return;
    const index = [...codePane.current.querySelectorAll('.code-line')].indexOf(line) + 1;
    const key = linkAtLine(links, index);
    if (!key) return;
    setSelected((current) => (current === key ? null : key));
    showLater('diagram');
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    const next = event.key === 'ArrowLeft' ? 'diagram' : 'code';
    setSide(next);
    document.getElementById(`${id}-tab-${next}`)?.focus();
  };

  return (
    <figure
      ref={figure}
      className="code-block compare uml-pair"
      data-layout={split ? 'split' : 'tabs'}
      aria-label="Code and UML"
    >
      <figcaption className="code-block-label compare-head">
        <span className="code-block-title">{split ? 'code ↔ uml' : side}</span>
        {split ? (
          <span>{code.language}</span>
        ) : (
          <span
            role="tablist"
            aria-label="Show"
            className="compare-switch"
            data-side={side === 'diagram' ? 'before' : 'after'}
            onKeyDown={onKeyDown}
          >
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
          const shown = split || side === name;
          return (
            <section
              key={name}
              ref={name === 'code' ? codePane : undefined}
              id={`${id}-${name}`}
              role={split ? undefined : 'tabpanel'}
              aria-labelledby={split ? undefined : `${id}-tab-${name}`}
              className={cn('compare-pane', `uml-pair-${name}`)}
              data-shown={shown || undefined}
              inert={!shown}
              aria-hidden={!shown || undefined}
              onClick={name === 'code' ? onCodeClick : undefined}
            >
              {name === 'diagram' ? (
                <MermaidDiagram source={diagram.code} selection={{ selected, onSelect: selectFromDiagram }} />
              ) : (
                <CodeLines
                  code={code.code}
                  info={code.language}
                  marks={marks}
                  markKind="mark"
                  linkable={linkable}
                  label="Paired"
                />
              )}
            </section>
          );
        })}
      </div>
      <p className="sr-only" aria-live="polite">
        {selected && links.has(selected) ? `${selected}: ${links.get(selected)!.size} lines of code` : ''}
      </p>
    </figure>
  );
}
