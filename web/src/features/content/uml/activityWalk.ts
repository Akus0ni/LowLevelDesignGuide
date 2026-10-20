import type { ActivityGraph } from './umlSource';

/**
 * Walking an activity diagram one step at a time (spec 008 §3.6). A step is the set of nodes that are active now and
 * the edges just taken. One node is active, except between a fork and its join, where every branch moves together.
 * Branches are expected to be straight lines (decisions sit outside forks, as in an interview sketch).
 */
export interface ActivityStep {
  active: string[];
  /** Indexes into `graph.edges` taken to get here. */
  via: number[];
}

export function firstStep(graph: ActivityGraph): ActivityStep | null {
  const targets = new Set(graph.edges.map((edge) => edge.to));
  const start =
    [...graph.nodes].find(([, kind]) => kind === 'start')?.[0] ??
    [...graph.nodes.keys()].find((id) => !targets.has(id));
  return start ? { active: [start], via: [] } : null;
}

function outOf(graph: ActivityGraph, node: string): number[] {
  return graph.edges.flatMap((edge, index) => (edge.from === node ? [index] : []));
}

/** The branches to choose from at a decision; empty when Next needs no choice. */
export function choicesAt(graph: ActivityGraph, step: ActivityStep): number[] {
  if (step.active.length !== 1) return [];
  const [node] = step.active;
  const out = outOf(graph, node);
  return out.length > 1 && graph.nodes.get(node) !== 'fork' ? out : [];
}

/** True when nothing follows: the walk reached an end node. */
export function isDone(graph: ActivityGraph, step: ActivityStep): boolean {
  return step.active.length === 1 && outOf(graph, step.active[0]).length === 0;
}

/**
 * The step after `step`. At a decision, `choice` (an edge index from `choicesAt`) says which branch; without it, there
 * is no next step. Returns null when the walk is done.
 */
export function nextStep(graph: ActivityGraph, step: ActivityStep, choice?: number): ActivityStep | null {
  if (isDone(graph, step)) return null;

  if (step.active.length === 1) {
    const [node] = step.active;
    const out = outOf(graph, node);
    if (graph.nodes.get(node) === 'fork') return { active: out.map((e) => graph.edges[e].to), via: out };
    if (out.length > 1) {
      if (choice === undefined || !out.includes(choice)) return null;
      return { active: [graph.edges[choice].to], via: [choice] };
    }
    return { active: [graph.edges[out[0]].to], via: out };
  }

  // Inside a fork: every branch that hasn't reached its join takes its next edge; the join waits for all of them.
  const via: number[] = [];
  const active = new Set<string>();
  for (const node of step.active) {
    if (graph.nodes.get(node) === 'join') {
      active.add(node);
      continue;
    }
    const [edge] = outOf(graph, node);
    if (edge === undefined) {
      active.add(node);
      continue;
    }
    via.push(edge);
    active.add(graph.edges[edge].to);
  }
  return { active: [...active], via };
}
