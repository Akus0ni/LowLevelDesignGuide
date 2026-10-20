/**
 * Reading Mermaid sources for the UML reader features (spec 008 §3.4–3.6). The SVG says where things are drawn; the
 * source says what they mean (which arrow, which event, which node is a fork). Parsing is line by line and never
 * throws: anything unrecognised is skipped, and the diagram then simply stays static.
 */

function lines(source: string): string[] {
  return source
    .split(/\r?\n/)
    .map((line) => line.replace(/%%.*$/, '').trim())
    .filter(Boolean);
}

// ---- Class diagrams -------------------------------------------------------------------------------------------

export interface ClassRelationship {
  /** As written without spaces or cardinalities, e.g. `Order*--OrderLine`; the same key the packer writes (spec 008 plan §2.4). */
  key: string;
  /** Left and right names as written (`Box~T~` → `Box`). */
  left: string;
  right: string;
  arrow: string;
}

/** Mirrors `MermaidClassDiagram.RelationshipLine` in Lld.Content.Authoring. */
const RELATIONSHIP =
  /^(?<a>[A-Za-z_][\w~]*)\s*(?:"[^"]*"\s*)?(?<!\w)(?<left><\||\*|o|<)?(?<body>--|\.\.)(?<right>\|>|\*|o|>)?(?!\w)\s*(?:"[^"]*"\s*)?(?<b>[A-Za-z_][\w~]*)\s*(?::.*)?$/;

const plain = (name: string) => name.split('~')[0];

/** Relationships in source order: Mermaid numbers its edges the same way (`id_A_B_1`, `id_A_B_2`…). */
export function classRelationships(source: string): ClassRelationship[] {
  const found: ClassRelationship[] = [];
  let depth = 0;
  for (const line of lines(source)) {
    if (depth > 0) {
      depth += count(line, '{') - count(line, '}');
      continue;
    }
    const match = RELATIONSHIP.exec(line);
    if (match?.groups) {
      const { a, b, left = '', body, right = '' } = match.groups;
      const arrow = `${left}${body}${right}`;
      found.push({ key: `${plain(a)}${arrow}${plain(b)}`, left: plain(a), right: plain(b), arrow });
    } else if (/^class\s/.test(line)) {
      depth = count(line, '{') - count(line, '}');
    }
  }
  return found;
}

function count(text: string, char: string): number {
  return text.split(char).length - 1;
}

/** "Order composes OrderLine": how an arrow reads aloud, for screen readers. */
export function describeRelationship({ left, right, arrow }: ClassRelationship): string {
  const verbs: [RegExp, (l: string, r: string) => string][] = [
    [/^<\|--$/, (l, r) => `${r} inherits from ${l}`],
    [/^--\|>$/, (l, r) => `${l} inherits from ${r}`],
    [/^<\|\.\.$/, (l, r) => `${r} implements ${l}`],
    [/^\.\.\|>$/, (l, r) => `${l} implements ${r}`],
    [/^\*--$/, (l, r) => `${l} is made of ${r}`],
    [/^--\*$/, (l, r) => `${r} is made of ${l}`],
    [/^o--$/, (l, r) => `${l} has ${r}`],
    [/^--o$/, (l, r) => `${r} has ${l}`],
    [/^-->$/, (l, r) => `${l} knows ${r}`],
    [/^<--$/, (l, r) => `${r} knows ${l}`],
    [/^\.\.>$/, (l, r) => `${l} uses ${r}`],
    [/^<\.\.$/, (l, r) => `${r} uses ${l}`],
  ];
  const verb = verbs.find(([pattern]) => pattern.test(arrow));
  return verb ? verb[1](left, right) : `${left} is linked to ${right}`;
}

// ---- State diagrams -------------------------------------------------------------------------------------------

/** The start / end pseudo-state. */
export const PSEUDO = '[*]';

export interface StateTransition {
  from: string;
  to: string;
  event: string | null;
  /** Inside a composite state (`state X { … }`): highlighted on tap, but not part of a run. */
  nested: boolean;
  /** Position in source order; Mermaid draws transition `edge<index>` in the same order. */
  index: number;
}

export interface StateGraph {
  states: string[];
  transitions: StateTransition[];
}

const TRANSITION = /^(\[\*\]|[A-Za-z_]\w*)\s*-->\s*(\[\*\]|[A-Za-z_]\w*)\s*(?::(.*))?$/;

export function stateGraph(source: string): StateGraph {
  const states = new Set<string>();
  const transitions: StateTransition[] = [];
  let depth = 0;
  for (const line of lines(source)) {
    const match = TRANSITION.exec(line);
    if (match) {
      const [, from, to, label] = match;
      for (const name of [from, to]) if (name !== PSEUDO) states.add(name);
      const event = label?.trim() || null;
      transitions.push({ from, to, event, nested: depth > 0, index: transitions.length });
    } else if (/^state\s.*\{\s*$/.test(line)) {
      depth++;
    } else if (line === '}') {
      depth = Math.max(0, depth - 1);
    }
  }
  return { states: [...states], transitions };
}

/** Where a run starts: the first top-level `[*] --> X`. */
export function initialState(graph: StateGraph): string | null {
  return graph.transitions.find((t) => !t.nested && t.from === PSEUDO)?.to ?? null;
}

/** What can happen next in a run; a transition to `[*]` ends it. */
export function outgoing(graph: StateGraph, state: string): StateTransition[] {
  return graph.transitions.filter((t) => !t.nested && t.from === state);
}

// ---- Activity flowcharts ----------------------------------------------------------------------------------------

export type ActivityKind = 'start' | 'end' | 'decision' | 'fork' | 'join' | 'action';

export interface ActivityEdge {
  from: string;
  to: string;
  label: string | null;
}

export interface ActivityGraph {
  nodes: Map<string, ActivityKind>;
  edges: ActivityEdge[];
}

/** One node in a chain: `id`, `id["text"]`, `id{"q"}`, `id((" "))`, `id(((" ")))`, `id[" "]:::fork`. */
const NODE = String.raw`([A-Za-z_][\w-]*)(\(\(\([^)]*\)\)\)|\(\([^)]*\)\)|\{[^}]*\}|\(\[[^\]]*\]\)|\[[^\]]*\]|\([^)]*\))?(?::::(\w+))?`;
const LINK = String.raw`\s*(?:-->|---|-\.->|==>)\s*(?:\|([^|]*)\|\s*)?`;
const NODE_AT_START = new RegExp(`^${NODE}`);
const LINK_AT_START = new RegExp(`^${LINK}`);

/**
 * The graph of an activity flowchart (spec 008 §3.6, CD-007): start `(( ))`, end `((( )))`, decisions `{ }`, forks
 * and joins as `:::fork` / `:::join` nodes. Chains (`a --> b --> c`) and edge labels (`-->|yes|`) are read.
 */
export function activityGraph(source: string): ActivityGraph {
  const nodes = new Map<string, ActivityKind>();
  const edges: ActivityEdge[] = [];
  for (const line of lines(source)) {
    if (/^(flowchart|graph|classDef|class|style|linkStyle|click|subgraph|end|direction)\b/.test(line)) continue;
    let rest = line;
    let previous: string | null = null;
    let label: string | null = null;
    while (rest.length > 0) {
      const node = NODE_AT_START.exec(rest);
      if (!node) break;
      const [text, id, shape, className] = node;
      const kind = kindOf(shape, className);
      if (!nodes.has(id) || (nodes.get(id) === 'action' && kind !== 'action')) nodes.set(id, kind);
      if (previous) edges.push({ from: previous, to: id, label });
      previous = id;
      rest = rest.slice(text.length);
      const link = LINK_AT_START.exec(rest);
      if (!link) break;
      label = unquote(link[1]?.trim() ?? '') || null;
      rest = rest.slice(link[0].length);
    }
  }
  return { nodes, edges };
}

/** `"yes"` → `yes`: labels may be quoted in the source. */
function unquote(text: string): string {
  return text.replace(/^"(.*)"$/, '$1');
}

function kindOf(shape: string | undefined, className: string | undefined): ActivityKind {
  if (className === 'fork' || className === 'join') return className;
  if (shape?.startsWith('(((')) return 'end';
  if (shape?.startsWith('((')) return 'start';
  if (shape?.startsWith('{')) return 'decision';
  return 'action';
}

/** The `classDef` the reader appends to a `steps` flowchart so `:::fork` / `:::join` become node classes (CSS draws the bars). */
export const ACTIVITY_CLASS_DEFS = '\n  classDef fork,join stroke-width:0px';
