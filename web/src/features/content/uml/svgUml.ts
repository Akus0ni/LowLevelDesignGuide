import { describeRelationship, type ActivityGraph, type ClassRelationship, type StateGraph } from './umlSource';

/**
 * Finding the parts of a rendered Mermaid SVG for the UML features (spec 008 §3.4–3.6), and marking them. Pure DOM
 * work, tested against real Mermaid 12 output (`markdown/__fixtures__`). Anything not found is simply left out.
 */

export interface DiagramParts {
  nodes: Map<string, Element>;
  /** Edge index (source order) → its path and, when labelled, its label. */
  edges: Map<number, Element[]>;
}

function labelsById(svg: SVGSVGElement): Map<string, Element> {
  const labels = new Map<string, Element>();
  for (const label of svg.querySelectorAll('.edgeLabel .label[data-id]')) {
    labels.set(label.getAttribute('data-id')!, label.closest('.edgeLabel') ?? label);
  }
  return labels;
}

function edgeElements(path: Element, labels: Map<string, Element>): Element[] {
  const label = labels.get(path.getAttribute('data-id') ?? '');
  return label ? [path, label] : [path];
}

// ---- Class diagrams ---------------------------------------------------------------------------------------------

const CLASS_EDGE = /^id_.+_(\d+)$/;

/** Relationship key → its path and label. Mermaid numbers class edges from 1 in source order (`id_Order_Customer_3`). */
export function classEdges(svg: SVGSVGElement, relationships: ClassRelationship[]): Map<string, Element[]> {
  const labels = labelsById(svg);
  const edges = new Map<string, Element[]>();
  for (const path of svg.querySelectorAll('path[data-et="edge"][data-id]')) {
    const number = CLASS_EDGE.exec(path.getAttribute('data-id') ?? '')?.[1];
    const relationship = number ? relationships[Number(number) - 1] : undefined;
    if (relationship && !edges.has(relationship.key)) edges.set(relationship.key, edgeElements(path, labels));
  }
  return edges;
}

/**
 * Gives each class edge a wide, invisible twin that takes taps and focus (a 1.5 px line is too thin to hit), so a
 * Code ↔ UML pair can light up the code behind an arrow. Returns the twins by key.
 */
export function makeEdgesTappable(
  edges: Map<string, Element[]>,
  relationships: ClassRelationship[],
): Map<string, Element> {
  const hits = new Map<string, Element>();
  for (const relationship of relationships) {
    const path = edges.get(relationship.key)?.[0];
    if (!path || !path.parentNode) continue;
    const hit = path.cloneNode(false) as Element;
    for (const name of ['id', 'marker-end', 'marker-start', 'style', 'data-et', 'data-id', 'data-edge'])
      hit.removeAttribute(name);
    hit.setAttribute('class', 'edge-hit');
    hit.setAttribute('data-edge-key', relationship.key);
    hit.setAttribute('tabindex', '0');
    hit.setAttribute('role', 'button');
    hit.setAttribute('aria-label', describeRelationship(relationship));
    path.parentNode.insertBefore(hit, path.nextSibling);
    hits.set(relationship.key, hit);
  }
  return hits;
}

// ---- State diagrams ---------------------------------------------------------------------------------------------

const STATE_NODE = /-state-(.+)-\d+$/;
const STATE_EDGE = /^edge(\d+)$/;

export function stateParts(svg: SVGSVGElement, graph: StateGraph): DiagramParts {
  const nodes = new Map<string, Element>();
  for (const node of svg.querySelectorAll('g.node[id*="-state-"]')) {
    const name = STATE_NODE.exec(node.id)?.[1];
    if (name && graph.states.includes(name)) nodes.set(name, node);
  }
  const labels = labelsById(svg);
  const edges = new Map<number, Element[]>();
  for (const path of svg.querySelectorAll('path[data-et="edge"][data-id]')) {
    const index = STATE_EDGE.exec(path.getAttribute('data-id') ?? '')?.[1];
    if (index !== undefined && Number(index) < graph.transitions.length) {
      edges.set(Number(index), edgeElements(path, labels));
    }
  }
  return { nodes, edges };
}

// ---- Activity flowcharts ----------------------------------------------------------------------------------------

const FLOW_NODE = /-flowchart-(.+)-\d+$/;
const FLOW_EDGE = /^L_(.+)_(\d+)$/;

export function flowParts(svg: SVGSVGElement, graph: ActivityGraph): DiagramParts {
  const nodes = new Map<string, Element>();
  for (const node of svg.querySelectorAll('g.node[id*="-flowchart-"]')) {
    const id = FLOW_NODE.exec(node.id)?.[1];
    if (id && graph.nodes.has(id)) nodes.set(id, node);
  }

  // `L_from_to_n`: ids may contain `_`, so only a split into two known ids counts; n counts repeats of the pair.
  const labels = labelsById(svg);
  const byPair = new Map<string, Element[]>();
  for (const path of svg.querySelectorAll('path[data-et="edge"][data-id]')) {
    const match = FLOW_EDGE.exec(path.getAttribute('data-id') ?? '');
    if (!match) continue;
    const pair = splitPair(match[1], graph);
    if (!pair) continue;
    const list = byPair.get(pair) ?? [];
    list[Number(match[2])] = path;
    byPair.set(pair, list);
  }

  const edges = new Map<number, Element[]>();
  const seen = new Map<string, number>();
  graph.edges.forEach((edge, index) => {
    const pair = `${edge.from}\u0000${edge.to}`;
    const n = seen.get(pair) ?? 0;
    seen.set(pair, n + 1);
    const path = byPair.get(pair)?.[n];
    if (path) edges.set(index, edgeElements(path, labels));
  });
  return { nodes, edges };
}

function splitPair(core: string, graph: ActivityGraph): string | null {
  for (let i = core.indexOf('_'); i > 0; i = core.indexOf('_', i + 1)) {
    const from = core.slice(0, i);
    const to = core.slice(i + 1);
    if (graph.nodes.has(from) && graph.nodes.has(to)) return `${from}\u0000${to}`;
  }
  return null;
}

// ---- Marking ----------------------------------------------------------------------------------------------------

const MARKS = ['data-related', 'data-selected', 'data-current', 'data-visited'] as const;

function clearMarks(svg: SVGSVGElement): void {
  for (const mark of MARKS) for (const element of svg.querySelectorAll(`[${mark}]`)) element.removeAttribute(mark);
}

/**
 * Focus: `related` (nodes and edges) stay, everything else steps back (the class-focus CSS); `selected` gets the
 * accent outline. An empty list clears.
 */
export function markFocus(svg: SVGSVGElement, related: Element[], selected: Element[] = []): void {
  clearMarks(svg);
  svg.toggleAttribute('data-focus', related.length > 0 || selected.length > 0);
  for (const element of related) element.setAttribute('data-related', '');
  for (const element of selected) {
    element.setAttribute('data-related', '');
    element.setAttribute('data-selected', '');
  }
}

/**
 * A run or a walk: `current` nodes carry the accent fill, the edges just taken are drawn in the accent colour,
 * `visited` nodes stay at full strength and the rest of the diagram waits, dimmed. `null` ends the run.
 */
export function markRun(
  svg: SVGSVGElement,
  run: { current: Element[]; taken: Element[]; visited: Element[] } | null,
): void {
  clearMarks(svg);
  svg.removeAttribute('data-focus');
  svg.toggleAttribute('data-running', run !== null);
  if (!run) return;
  for (const element of run.visited) element.setAttribute('data-visited', '');
  for (const element of run.taken) element.setAttribute('data-related', '');
  for (const element of run.current) element.setAttribute('data-current', '');
}
