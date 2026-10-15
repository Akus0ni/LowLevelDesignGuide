import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode, type RefObject } from 'react';
import { cn } from '../../../ui/cn';
import { Icon } from '../../../ui/Icon';
import {
  analyzeClassDiagram,
  analyzeSequence,
  applyClassFocus,
  applySequenceStep,
  classNameOf,
  kindOf,
  makeClassesFocusable,
  neighboursOf,
  type ClassModel,
  type SequenceStep,
} from './interactive';

const previousIcon = ['m15 6-6 6 6 6'];
const nextIcon = ['m9 6 6 6-6 6'];

/**
 * A rendered diagram with its top strip. Class diagrams: tap (or Enter on) a class to trace its relationships.
 * Sequence diagrams: "step through" reveals messages one at a time (‹ › or ← →). Anything else stays static.
 * Used inline and in the full-screen viewer; each keeps its own state.
 */
export function InteractiveDiagram({
  svg,
  label,
  actions,
  className,
  canvasClassName,
  showLabel = true,
}: {
  svg: string;
  label: string;
  actions?: ReactNode;
  className: string;
  canvasClassName: string;
  /** The viewer's header already names the diagram. */
  showLabel?: boolean;
}) {
  const canvas = useRef<HTMLDivElement>(null);
  const classes = useClassFocus(canvas, svg);
  const sequence = useSequenceSteps(canvas, svg);

  const onKeyDown = (event: KeyboardEvent) => {
    if (sequence.current === null) return;
    if (event.key === 'ArrowRight') sequence.go(1);
    else if (event.key === 'ArrowLeft') sequence.go(-1);
    else if (event.key === 'Escape') sequence.showAll();
    else return;
    event.preventDefault();
  };

  return (
    <figure className={className} aria-label={label} onKeyDown={onKeyDown}>
      <figcaption className="diagram-label">
        <span className="min-w-0 truncate">{showLabel ? label : ''}</span>
        <span className="flex shrink-0 items-center gap-1">
          {classes.available && (
            <ClassHint selected={classes.selected} related={classes.related} onClear={classes.clear} />
          )}
          {sequence.count > 0 && <StepControls sequence={sequence} />}
          {actions}
        </span>
      </figcaption>
      <div
        ref={canvas}
        className={canvasClassName}
        // Mermaid's strict security level sanitises the SVG.
        dangerouslySetInnerHTML={{ __html: svg }}
      />
      <p className="sr-only" aria-live="polite">
        {sequence.current !== null
          ? `Step ${sequence.current + 1} of ${sequence.count}: ${sequence.steps[sequence.current]?.text ?? ''}`
          : classes.selected
            ? `${classes.selected}: connected to ${classes.related.join(', ') || 'nothing'}`
            : ''}
      </p>
    </figure>
  );
}

function svgIn(canvas: RefObject<HTMLDivElement | null>): SVGSVGElement | null {
  return canvas.current?.querySelector('svg') ?? null;
}

function useClassFocus(canvas: RefObject<HTMLDivElement | null>, svg: string) {
  const [model, setModel] = useState<ClassModel | null>(null);
  const [selected, setSelected] = useState<string | null>(null);

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || kindOf(root) !== 'class') return;
    const found = analyzeClassDiagram(root);
    if (found.nodes.size === 0) return;
    makeClassesFocusable(found);
    setModel(found);

    const toggle = (name: string | null) =>
      setSelected((current) => (name && name !== current ? name : null));
    const onClick = (event: Event) => toggle(classNameOf(found, event.target));
    const onKey = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') setSelected(null);
      else if (event.key === 'Enter' || event.key === ' ') {
        const name = classNameOf(found, event.target);
        if (!name) return;
        event.preventDefault();
        toggle(name);
      }
    };
    root.addEventListener('click', onClick);
    root.addEventListener('keydown', onKey);
    return () => {
      root.removeEventListener('click', onClick);
      root.removeEventListener('keydown', onKey);
    };
  }, [canvas, svg]);

  useEffect(() => {
    const root = svgIn(canvas);
    if (root && model) applyClassFocus(root, model, selected);
  }, [canvas, model, selected]);

  return {
    available: model !== null,
    selected,
    related: model && selected ? neighboursOf(model, selected) : [],
    clear: () => setSelected(null),
  };
}

function useSequenceSteps(canvas: RefObject<HTMLDivElement | null>, svg: string) {
  const [steps, setSteps] = useState<SequenceStep[]>([]);
  const [current, setCurrent] = useState<number | null>(null);

  useEffect(() => {
    const root = svgIn(canvas);
    if (root && kindOf(root) === 'sequence') setSteps(analyzeSequence(root));
  }, [canvas, svg]);

  useEffect(() => {
    const root = svgIn(canvas);
    if (root && steps.length > 0) applySequenceStep(root, steps, current);
  }, [canvas, steps, current]);

  return {
    steps,
    count: steps.length,
    current,
    start: () => setCurrent(0),
    go: (delta: number) =>
      setCurrent((index) => Math.min(steps.length - 1, Math.max(0, (index ?? -1) + delta))),
    showAll: () => setCurrent(null),
  };
}

type Sequence = ReturnType<typeof useSequenceSteps>;

const stripButton = cn(
  'inline-flex h-8 min-w-8 items-center justify-center gap-1 rounded-md px-2 font-mono text-[11px] tracking-[0.06em] text-muted select-none',
  'transition-[transform,color,background-color] duration-[120ms] ease-out active:scale-[0.94] hover:text-fg',
  'focus-visible:outline-2 focus-visible:outline-accent disabled:pointer-events-none disabled:opacity-40',
);

function ClassHint({
  selected,
  related,
  onClear,
}: {
  selected: string | null;
  related: string[];
  onClear: () => void;
}) {
  if (!selected) return <span className="hidden px-2 sm:inline">tap a class to trace it</span>;
  return (
    <>
      <span className="px-1 text-accent">
        {selected} · {related.length} link{related.length === 1 ? '' : 's'}
      </span>
      <button type="button" className={stripButton} onClick={onClear}>
        clear
      </button>
    </>
  );
}

function StepControls({ sequence }: { sequence: Sequence }) {
  if (sequence.current === null) {
    return (
      <button type="button" className={stripButton} onClick={sequence.start}>
        step through
      </button>
    );
  }
  return (
    <>
      <button
        type="button"
        aria-label="Previous message"
        className={stripButton}
        disabled={sequence.current === 0}
        onClick={() => sequence.go(-1)}
      >
        <Icon d={previousIcon} size={14} />
      </button>
      <span className="min-w-[3.25rem] text-center tabular-nums text-fg">
        {sequence.current + 1}/{sequence.count}
      </span>
      <button
        type="button"
        aria-label="Next message"
        className={stripButton}
        disabled={sequence.current === sequence.count - 1}
        onClick={() => sequence.go(1)}
      >
        <Icon d={nextIcon} size={14} />
      </button>
      <button type="button" className={stripButton} onClick={sequence.showAll}>
        all
      </button>
    </>
  );
}
