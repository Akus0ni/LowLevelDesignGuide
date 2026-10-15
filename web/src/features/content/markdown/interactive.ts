/**
 * Interactive diagrams (spec 003 §3.8): pure DOM work on the SVG Mermaid renders, so it can be tested against real
 * Mermaid output (`__fixtures__`). If the markup isn't recognised, the analysis finds nothing and the diagram simply
 * stays static.
 */

export type DiagramKind = 'class' | 'sequence' | 'other';

/** Which kind of diagram an SVG is, from the role Mermaid writes on it. */
export function kindOf(svg: SVGSVGElement): DiagramKind {
  const role = svg.getAttribute('aria-roledescription') ?? '';
  if (role.startsWith('class')) return 'class';
  if (role === 'sequence') return 'sequence';
  return 'other';
}

// ---- Class diagrams: tap a class to see what it is connected to -----------------------------------------------

export interface ClassModel {
  nodes: Map<string, Element>;
  /** Relationship elements (path + its label) and the two classes each one joins. */
  edges: { from: string; to: string; elements: Element[] }[];
}

const CLASS_NODE_ID = /-classId-(.+)-\d+$/;

export function analyzeClassDiagram(svg: SVGSVGElement): ClassModel {
  const nodes = new Map<string, Element>();
  for (const node of svg.querySelectorAll('g.node[id*="-classId-"]')) {
    const name = CLASS_NODE_ID.exec(node.id)?.[1];
    if (name) nodes.set(name, node);
  }

  const labels = new Map<string, Element>();
  for (const label of svg.querySelectorAll('.edgeLabel .label[data-id]')) {
    labels.set(label.getAttribute('data-id')!, label.closest('.edgeLabel') ?? label);
  }

  const edges: ClassModel['edges'] = [];
  for (const path of svg.querySelectorAll('path[data-et="edge"][data-id]')) {
    const id = path.getAttribute('data-id')!;
    const ends = splitEnds(id, nodes);
    if (!ends) continue;
    const label = labels.get(id);
    edges.push({ ...ends, elements: label ? [path, label] : [path] });
  }

  return { nodes, edges };
}

/** `id_Login_SessionManager_5` → Login, SessionManager. Names may contain `_`, so only known class names count. */
function splitEnds(id: string, nodes: Map<string, Element>): { from: string; to: string } | null {
  const core = id.replace(/^id_/, '').replace(/_\d+$/, '');
  for (let i = core.indexOf('_'); i > 0; i = core.indexOf('_', i + 1)) {
    const from = core.slice(0, i);
    const to = core.slice(i + 1);
    if (nodes.has(from) && nodes.has(to)) return { from, to };
  }
  return null;
}

/** Classes directly connected to `name`. */
export function neighboursOf(model: ClassModel, name: string): string[] {
  const neighbours = new Set<string>();
  for (const edge of model.edges) {
    if (edge.from === name) neighbours.add(edge.to);
    if (edge.to === name) neighbours.add(edge.from);
  }
  neighbours.delete(name);
  return [...neighbours].sort();
}

/** Marks the focused class, its relationships and its neighbours; everything else is dimmed by CSS. */
export function applyClassFocus(svg: SVGSVGElement, model: ClassModel, name: string | null): void {
  for (const element of svg.querySelectorAll('[data-related], [data-selected]')) {
    element.removeAttribute('data-related');
    element.removeAttribute('data-selected');
  }
  for (const [className, node] of model.nodes) node.setAttribute('aria-pressed', String(className === name));

  const node = name ? model.nodes.get(name) : undefined;
  if (!name || !node) {
    svg.removeAttribute('data-focus');
    return;
  }

  svg.setAttribute('data-focus', '');
  node.setAttribute('data-selected', '');
  node.setAttribute('data-related', '');
  for (const edge of model.edges) {
    if (edge.from !== name && edge.to !== name) continue;
    edge.elements.forEach((element) => element.setAttribute('data-related', ''));
    model.nodes.get(edge.from === name ? edge.to : edge.from)?.setAttribute('data-related', '');
  }
}

/** Makes every class a keyboard-reachable toggle button. */
export function makeClassesFocusable(model: ClassModel): void {
  for (const [name, node] of model.nodes) {
    node.setAttribute('tabindex', '0');
    node.setAttribute('role', 'button');
    node.setAttribute('aria-label', `Class ${name}`);
    node.setAttribute('aria-pressed', 'false');
  }
}

export function classNameOf(model: ClassModel, target: EventTarget | null): string | null {
  const node = target instanceof Element ? target.closest('g.node') : null;
  if (!node) return null;
  for (const [name, candidate] of model.nodes) if (candidate === node) return name;
  return null;
}

// ---- Sequence diagrams: step through the messages ----------------------------------------------------------------

export interface SequenceStep {
  elements: Element[];
  /** What the step says, for screen readers ("POST /api/access-requests"). */
  text: string;
}

/** The numbered dot is a zero-length line whose start marker draws the circle. */
const NUMBER_DOT = 'line[marker-start*="sequencenumber"]';
const STEP_PARTS = `.messageText, .messageLine0, .messageLine1, ${NUMBER_DOT}, .sequenceNumber, .note, .noteText`;

/**
 * Messages and notes in the order they happen. Mermaid writes a message's text, then its line, then its number dot
 * and number;
 * a note is a box followed by its text. Lifelines, actors and blocks stay visible all the time.
 */
export function analyzeSequence(svg: SVGSVGElement): SequenceStep[] {
  const steps: SequenceStep[] = [];
  let pendingText: Element[] = [];
  let last: SequenceStep | null = null;

  for (const element of svg.querySelectorAll(STEP_PARTS)) {
    const kind = element.getAttribute('class') ?? '';
    if (kind.includes('messageText')) {
      pendingText.push(element);
    } else if (kind.includes('messageLine')) {
      last = { elements: [...pendingText, element], text: textOf(pendingText) };
      steps.push(last);
      pendingText = [];
    } else if (kind.includes('sequenceNumber') || kind.includes('noteText') || element.matches(NUMBER_DOT)) {
      last?.elements.push(element);
      if (kind.includes('noteText') && last) last.text = `Note: ${element.textContent?.trim() ?? ''}`;
    } else if (kind.split(/\s+/).includes('note')) {
      last = { elements: [element], text: 'Note' };
      steps.push(last);
    }
  }
  return steps;
}

function textOf(elements: Element[]): string {
  return elements
    .map((element) => element.textContent?.trim() ?? '')
    .filter(Boolean)
    .join(' ');
}

/** Shows steps up to `index` (the current one highlighted); `null` shows the whole diagram. */
export function applySequenceStep(svg: SVGSVGElement, steps: SequenceStep[], index: number | null): void {
  svg.toggleAttribute('data-stepping', index !== null);
  steps.forEach((step, i) => {
    const state = index === null ? null : i < index ? 'past' : i === index ? 'current' : 'future';
    for (const element of step.elements) {
      if (state) element.setAttribute('data-step', state);
      else element.removeAttribute('data-step');
    }
  });
}
