import { useEffect, useMemo, useRef, useState, type RefObject } from 'react';
import { choicesAt, firstStep, isDone, nextStep, type ActivityStep } from './activityWalk';
import { flowParts, markFocus, markRun, stateParts, type DiagramParts } from './svgUml';
import {
  PSEUDO,
  activityGraph,
  initialState,
  outgoing,
  stateGraph,
  type StateTransition,
} from './umlSource';

function svgIn(canvas: RefObject<HTMLDivElement | null>): SVGSVGElement | null {
  return canvas.current?.querySelector('svg') ?? null;
}

/** Selection owned by a parent (the Code ↔ UML pair), so a tap in the diagram can light up its code. */
export interface DiagramSelection {
  selected: string | null;
  onSelect: (key: string | null) => void;
}

// ---- State diagrams: tap a state, or run the machine ------------------------------------------------------------

interface RunStep {
  /** The state reached; `[*]` once a final transition was taken. */
  state: string;
  /** The transition taken to get here; null for the initial state. */
  via: number | null;
}

/**
 * Spec 008 §3.6: tap a state to trace its transitions; Run starts at the initial state and fires one event at a time
 * until a final state. Works on every `stateDiagram-v2` (CD-004). With `selection`, taps report to the pair instead.
 */
export function useStateDiagram(
  canvas: RefObject<HTMLDivElement | null>,
  svg: string,
  source: string,
  selection?: DiagramSelection,
) {
  const graph = useMemo(() => stateGraph(source), [source]);
  const [parts, setParts] = useState<DiagramParts | null>(null);
  const [ownFocus, setOwnFocus] = useState<string | null>(null);
  const [run, setRun] = useState<RunStep[] | null>(null);
  const focused = selection ? (graph.states.includes(selection.selected ?? '') ? selection.selected : null) : ownFocus;
  const latest = useRef({ selection, running: false });
  latest.current = { selection, running: run !== null };

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || root.getAttribute('aria-roledescription') !== 'stateDiagram') return;
    const found = stateParts(root, graph);
    if (found.nodes.size === 0) return;
    for (const [name, node] of found.nodes) {
      node.setAttribute('tabindex', '0');
      node.setAttribute('role', 'button');
      node.setAttribute('aria-label', `State ${name}`);
    }
    setParts(found);

    const nameOf = (target: EventTarget | null) => {
      const node = target instanceof Element ? target.closest('g.node') : null;
      for (const [name, candidate] of found.nodes) if (candidate === node) return name;
      return null;
    };
    const toggle = (name: string | null) => {
      if (latest.current.running) return; // A run owns the marks until it stops.
      const own = latest.current.selection;
      if (own) own.onSelect(name && name !== own.selected ? name : null);
      else setOwnFocus((current) => (name && name !== current ? name : null));
    };
    const onClick = (event: Event) => toggle(nameOf(event.target));
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') toggle(null);
      else if ((event.key === 'Enter' || event.key === ' ') && nameOf(event.target)) {
        event.preventDefault();
        toggle(nameOf(event.target));
      }
    };
    root.addEventListener('click', onClick);
    root.addEventListener('keydown', onKey);
    return () => {
      root.removeEventListener('click', onClick);
      root.removeEventListener('keydown', onKey);
    };
  }, [canvas, svg, graph]);

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || !parts) return;
    if (run) {
      const last = run[run.length - 1];
      const end = root.querySelector('g.node[id*="-state-root_end-"]');
      const current = last.state === PSEUDO ? (end ? [end] : []) : [parts.nodes.get(last.state)].filter(isElement);
      const visited = run.slice(0, -1).flatMap((step) => [parts.nodes.get(step.state)].filter(isElement));
      const taken = last.via === null ? [] : (parts.edges.get(last.via) ?? []);
      markRun(root, { current, taken, visited });
      return;
    }
    markRun(root, null);
    if (!focused) {
      markFocus(root, []);
      return;
    }
    const touching = graph.transitions.filter((t) => t.from === focused || t.to === focused);
    const related = touching.flatMap((t) => [
      ...(parts.edges.get(t.index) ?? []),
      ...[parts.nodes.get(t.from), parts.nodes.get(t.to)].filter(isElement),
    ]);
    markFocus(root, related, [parts.nodes.get(focused)].filter(isElement));
  }, [canvas, parts, run, focused, graph]);

  const initial = initialState(graph);
  const current = run?.[run.length - 1] ?? null;
  return {
    available: parts !== null,
    canRun: parts !== null && initial !== null,
    focused,
    linked: focused ? new Set(graph.transitions.filter((t) => t.from === focused || t.to === focused).flatMap((t) => [t.from, t.to])).size - 1 : 0,
    clearFocus: () => (selection ? selection.onSelect(null) : setOwnFocus(null)),
    run: current,
    steps: run?.length ?? 0,
    done: current?.state === PSEUDO || (current !== null && outgoing(graph, current.state).length === 0),
    events: current && current.state !== PSEUDO ? outgoing(graph, current.state) : [],
    start: () => {
      if (!initial) return;
      if (selection) selection.onSelect(null);
      setOwnFocus(null);
      setRun([{ state: initial, via: null }]);
    },
    fire: (transition: StateTransition) =>
      setRun((steps) => (steps ? [...steps, { state: transition.to, via: transition.index }] : steps)),
    back: () => setRun((steps) => (steps && steps.length > 1 ? steps.slice(0, -1) : steps)),
    stop: () => setRun(null),
  };
}

export type StateDiagram = ReturnType<typeof useStateDiagram>;

function isElement(value: Element | undefined): value is Element {
  return value !== undefined;
}

// ---- Activity diagrams: step through ------------------------------------------------------------------------------

/**
 * Spec 008 §3.6: a `mermaid steps` flowchart can be walked node by node. Nodes get their UML kind as `data-kind` (CSS
 * draws start / end dots and fork / join bars) as soon as the diagram is drawn, stepping or not.
 */
export function useActivitySteps(canvas: RefObject<HTMLDivElement | null>, svg: string, source: string, enabled: boolean) {
  const graph = useMemo(() => activityGraph(source), [source]);
  const [parts, setParts] = useState<DiagramParts | null>(null);
  const [history, setHistory] = useState<ActivityStep[] | null>(null);

  useEffect(() => {
    const root = svgIn(canvas);
    if (!enabled || !root) return;
    const found = flowParts(root, graph);
    if (found.nodes.size === 0) return;
    for (const [id, node] of found.nodes) node.setAttribute('data-kind', graph.nodes.get(id) ?? 'action');
    setParts(found);
  }, [canvas, svg, graph, enabled]);

  useEffect(() => {
    const root = svgIn(canvas);
    if (!root || !parts) return;
    if (!history) {
      markRun(root, null);
      return;
    }
    const step = history[history.length - 1];
    const nodesOf = (ids: string[]) => ids.map((id) => parts.nodes.get(id)).filter(isElement);
    markRun(root, {
      current: nodesOf(step.active),
      taken: step.via.flatMap((edge) => parts.edges.get(edge) ?? []),
      visited: nodesOf(history.slice(0, -1).flatMap((s) => s.active)),
    });
  }, [canvas, parts, history]);

  const step = history?.[history.length - 1] ?? null;
  const labelOf = (edge: number) => {
    const { label, to } = graph.edges[edge];
    return label ?? parts?.nodes.get(to)?.textContent?.trim() ?? to;
  };
  return {
    available: enabled && parts !== null && firstStep(graph) !== null,
    stepping: step !== null,
    index: history ? history.length : 0,
    choices: step ? choicesAt(graph, step).map((edge) => ({ edge, label: labelOf(edge) })) : [],
    done: step !== null && isDone(graph, step),
    start: () => {
      const first = firstStep(graph);
      if (first) setHistory([first]);
    },
    next: (choice?: number) =>
      setHistory((steps) => {
        const last = steps?.[steps.length - 1];
        const following = last ? nextStep(graph, last, choice) : null;
        return steps && following ? [...steps, following] : steps;
      }),
    back: () => setHistory((steps) => (steps && steps.length > 1 ? steps.slice(0, -1) : steps)),
    stop: () => setHistory(null),
  };
}

export type ActivitySteps = ReturnType<typeof useActivitySteps>;
