import { useEffect, useMemo, useRef, useState, type KeyboardEvent, type ReactNode, type RefObject } from 'react';
import { Icon } from '../../../ui/Icon';
import { ArrowLegendButton } from '../uml/ArrowLegend';
import { ActivityBar, ActivityStrip, StateRunBar, StateStrip } from '../uml/DiagramControls';
import { stripButton } from '../uml/stripButton';
import { classEdges, makeEdgesTappable, markFocus } from '../uml/svgUml';
import { classRelationships } from '../uml/umlSource';
import { useActivitySteps, useStateDiagram, type DiagramSelection } from '../uml/useUmlDiagrams';
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
 * A rendered diagram with its top strip. Class diagrams: tap (or Enter on) a class to trace its relationships, and
 * "arrows" explains the six arrows (spec 008 §3.5). Sequence diagrams: "step through" reveals messages one at a time
 * (‹ › or ← →). State diagrams: tap a state, or "run" the machine event by event (spec 008 §3.6). Flowcharts marked
 * `steps`: walk the activity node by node. Anything else stays static. Used inline and in the full-screen viewer;
 * each keeps its own state. With `selection`, taps on classes, arrows and states report to a Code ↔ UML pair.
 */
export function InteractiveDiagram({
  svg,
  source,
  steps = false,
  selection,
  label,
  actions,
  className,
  canvasClassName,
  showLabel = true,
}: {
  svg: string;
  /** The Mermaid source: which arrow, event or fork each drawn part is. */
  source: string;
  /** An activity diagram to walk (`mermaid steps`). */
  steps?: boolean;
  selection?: DiagramSelection;
  label: string;
  actions?: ReactNode;
  className: string;
  canvasClassName: string;
  /** The viewer's header already names the diagram. */
  showLabel?: boolean;
}) {
  const canvas = useRef<HTMLDivElement>(null);
  const classes = useClassFocus(canvas, svg, source, selection);
  const sequence = useSequenceSteps(canvas, svg);
  const machine = useStateDiagram(canvas, svg, source, selection);
  const walk = useActivitySteps(canvas, svg, source, steps);

  const onKeyDown = (event: KeyboardEvent) => {
    if (sequence.current !== null) {
      if (event.key === 'ArrowRight') sequence.go(1);
      else if (event.key === 'ArrowLeft') sequence.go(-1);
      else if (event.key === 'Escape') sequence.showAll();
      else return;
    } else if (walk.stepping) {
      if (event.key === 'ArrowRight' && walk.choices.length === 0) walk.next();
      else if (event.key === 'ArrowLeft') walk.back();
      else if (event.key === 'Escape') walk.stop();
      else return;
    } else if (machine.run) {
      if (event.key === 'ArrowLeft') machine.back();
      else if (event.key === 'Escape') machine.stop();
      else return;
    } else return;
    event.preventDefault();
  };

  return (
    <figure className={className} aria-label={label} onKeyDown={onKeyDown}>
      <figcaption className="diagram-label">
        <span className="min-w-0 truncate">{showLabel ? label : ''}</span>
        <span className="flex shrink-0 items-center gap-1">
          {classes.available && (
            <ClassHint
              selected={classes.selected}
              related={classes.related}
              onClear={classes.clear}
              hint={selection ? 'tap a box or arrow' : 'tap a class to trace it'}
            />
          )}
          {classes.available && <ArrowLegendButton />}
          {sequence.count > 0 && <StepControls sequence={sequence} />}
          {machine.available && <StateStrip machine={machine} />}
          {walk.available && <ActivityStrip walk={walk} />}
          {actions}
        </span>
      </figcaption>
      <div
        ref={canvas}
        className={canvasClassName}
        // Mermaid's strict security level sanitises the SVG.
        dangerouslySetInnerHTML={{ __html: svg }}
      />
      {machine.available && <StateRunBar machine={machine} />}
      {walk.available && <ActivityBar walk={walk} />}
      <p className="sr-only" aria-live="polite">
        {sequence.current !== null
          ? `Step ${sequence.current + 1} of ${sequence.count}: ${sequence.steps[sequence.current]?.text ?? ''}`
          : machine.run
            ? machine.done
              ? 'Final state reached.'
              : `In state ${machine.run.state}. Events: ${machine.events.map((t) => t.event ?? 'next').join(', ')}`
            : walk.stepping
              ? `Step ${walk.index}${walk.choices.length > 0 ? `: choose ${walk.choices.map((c) => c.label).join(' or ')}` : ''}`
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

function useClassFocus(
  canvas: RefObject<HTMLDivElement | null>,
  svg: string,
  source: string,
  selection?: DiagramSelection,
) {
  const [model, setModel] = useState<ClassModel | null>(null);
  const [edges, setEdges] = useState<Map<string, Element[]>>(new Map());
  const [ownSelected, setOwnSelected] = useState<string | null>(null);
  const relationships = useMemo(() => classRelationships(source), [source]);
  const selected = selection ? selection.selected : ownSelected;
  const latest = useRef(selection);
  latest.current = selection;
  const paired = selection !== undefined;

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || kindOf(root) !== 'class') return;
    const found = analyzeClassDiagram(root);
    if (found.nodes.size === 0) return;
    makeClassesFocusable(found);
    setModel(found);
    // In a Code ↔ UML pair an arrow stands for code too, so arrows take taps.
    const byKey = classEdges(root, relationships);
    if (paired) makeEdgesTappable(byKey, relationships);
    setEdges(byKey);

    const toggle = (key: string | null) => {
      const own = latest.current;
      if (own) own.onSelect(key && key !== own.selected ? key : null);
      else setOwnSelected((current) => (key && key !== current ? key : null));
    };
    const keyOf = (target: EventTarget | null) =>
      target instanceof Element && target.matches('.edge-hit')
        ? target.getAttribute('data-edge-key')
        : classNameOf(found, target);
    const onClick = (event: Event) => toggle(keyOf(event.target));
    const onKey = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') toggle(null);
      else if (event.key === 'Enter' || event.key === ' ') {
        const key = keyOf(event.target);
        if (!key) return;
        event.preventDefault();
        toggle(key);
      }
    };
    root.addEventListener('click', onClick);
    root.addEventListener('keydown', onKey);
    return () => {
      root.removeEventListener('click', onClick);
      root.removeEventListener('keydown', onKey);
    };
  }, [canvas, svg, relationships, paired]);

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || !model) return;
    const relationship = relationships.find((r) => r.key === selected);
    const drawn = relationship ? edges.get(relationship.key) : undefined;
    if (relationship && drawn) {
      // An arrow: the arrow and its two classes stay, the rest steps back.
      const ends = [model.nodes.get(relationship.left), model.nodes.get(relationship.right)].filter(
        (node): node is Element => node !== undefined,
      );
      markFocus(root, [...drawn, ...ends], [drawn[0]]);
      return;
    }
    applyClassFocus(root, model, selected && model.nodes.has(selected) ? selected : null);
  }, [canvas, model, edges, relationships, selected]);

  const relationship = relationships.find((r) => r.key === selected);
  const isClass = model !== null && selected !== null && model.nodes.has(selected);
  return {
    available: model !== null,
    selected: relationship ? `${relationship.left} ${relationship.arrow} ${relationship.right}` : isClass ? selected : null,
    related: isClass ? neighboursOf(model, selected) : [],
    clear: () => (latest.current ? latest.current.onSelect(null) : setOwnSelected(null)),
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

function ClassHint({
  selected,
  related,
  onClear,
  hint,
}: {
  selected: string | null;
  related: string[];
  onClear: () => void;
  hint: string;
}) {
  if (!selected) return <span className="hidden px-2 sm:inline">{hint}</span>;
  return (
    <>
      <span className="max-w-[11rem] truncate px-1 text-accent">
        {selected}
        {related.length > 0 && ` · ${related.length} link${related.length === 1 ? '' : 's'}`}
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
