import { describe, expect, it } from 'vitest';
import activitySvg from '../markdown/__fixtures__/uml-activity.svg?raw';
import authStateSvg from '../markdown/__fixtures__/auth-state.svg?raw';
import classSvg from '../markdown/__fixtures__/uml-class-arrows.svg?raw';
import stateSvg from '../markdown/__fixtures__/uml-state.svg?raw';
import { choicesAt, firstStep, isDone, nextStep } from './activityWalk';
import { linkAtLine, parseCodeLinks } from './codeLinks';
import { classEdges, flowParts, makeEdgesTappable, markFocus, markRun, stateParts } from './svgUml';
import {
  activityGraph,
  classRelationships,
  describeRelationship,
  initialState,
  outgoing,
  stateGraph,
} from './umlSource';

/** The same sources `scripts/render-mermaid-fixtures.mjs` rendered. */
const CLASS_SOURCE = `classDiagram
  IEntity <|.. Order
  Order *-- OrderLine
  Order --> Customer
  Order o-- Discount
  Order ..> TaxPolicy
  Customer <|-- VipCustomer`;
const STATE_SOURCE = `stateDiagram-v2
  [*] --> Draft
  Draft --> InReview : submit
  InReview --> Draft : reject
  InReview --> Published : approve
  Published --> [*]`;
const AUTH_STATE_SOURCE = `stateDiagram-v2
    [*] --> Pending: Submit
    Pending --> Pending: Merge (same email asks again)
    Pending --> Approved: Approve (account created, temp password issued)
    Pending --> Rejected: Reject
    Approved --> [*]
    Rejected --> [*]
    note right of Approved
        Approved and Rejected are final
    end note`;
const ACTIVITY_SOURCE = `flowchart TD
  start((" ")) --> edit["Edit a note"]
  edit --> online{"Online?"}
  online -->|yes| fork1[" "]:::fork
  online -->|no| queue["Queue the change"]
  queue --> stop(((" ")))
  fork1 --> save["Save on device"]
  fork1 --> send["Send to server"]
  save --> join1[" "]:::join
  send --> join1
  join1 --> stop`;

function load(markup: string): SVGSVGElement {
  const host = document.createElement('div');
  host.innerHTML = markup;
  return host.querySelector('svg')!;
}

describe('class relationships', () => {
  it('reads every arrow with the key the packer writes', () => {
    expect(classRelationships(CLASS_SOURCE).map((r) => r.key)).toEqual([
      'IEntity<|..Order',
      'Order*--OrderLine',
      'Order-->Customer',
      'Ordero--Discount',
      'Order..>TaxPolicy',
      'Customer<|--VipCustomer',
    ]);
  });

  it('skips class bodies and reads cardinalities', () => {
    const source = 'classDiagram\n  class A {\n    +B b\n  }\n  A "1" *-- "many" B : has';
    expect(classRelationships(source).map((r) => r.key)).toEqual(['A*--B']);
  });

  it('says each arrow aloud', () => {
    const [realizes, composes, knows] = classRelationships(CLASS_SOURCE);
    expect(describeRelationship(realizes)).toBe('Order implements IEntity');
    expect(describeRelationship(composes)).toBe('Order is made of OrderLine');
    expect(describeRelationship(knows)).toBe('Order knows Customer');
  });

  it('finds each edge in the real SVG and gives it a tappable twin', () => {
    const svg = load(classSvg);
    const relationships = classRelationships(CLASS_SOURCE);
    const edges = classEdges(svg, relationships);
    expect([...edges.keys()]).toEqual(relationships.map((r) => r.key));
    expect(edges.get('Order-->Customer')![0].getAttribute('data-id')).toBe('id_Order_Customer_3');

    const hits = makeEdgesTappable(edges, relationships);
    const hit = hits.get('Order*--OrderLine')!;
    expect(hit.getAttribute('class')).toBe('edge-hit');
    expect(hit.getAttribute('aria-label')).toBe('Order is made of OrderLine');
    expect(hit.hasAttribute('marker-end')).toBe(false);
    expect(hit.previousElementSibling).toBe(edges.get('Order*--OrderLine')![0]);
  });
});

describe('code links', () => {
  it('parses the packed attribute and picks the narrowest link for a line', () => {
    const links = parseCodeLinks('Order=1-8;OrderLine=10;Order*--OrderLine=3;bad;=4');
    expect([...links.keys()]).toEqual(['Order', 'OrderLine', 'Order*--OrderLine']);
    expect(linkAtLine(links, 3)).toBe('Order*--OrderLine');
    expect(linkAtLine(links, 5)).toBe('Order');
    expect(linkAtLine(links, 9)).toBeNull();
  });
});

describe('state diagrams', () => {
  it('reads states, events and where a run starts', () => {
    const graph = stateGraph(STATE_SOURCE);
    expect(graph.states).toEqual(['Draft', 'InReview', 'Published']);
    expect(initialState(graph)).toBe('Draft');
    expect(outgoing(graph, 'InReview').map((t) => t.event)).toEqual(['reject', 'approve']);
    expect(outgoing(graph, 'Published').map((t) => t.to)).toEqual(['[*]']);
  });

  it('keeps transitions inside a composite state out of the run', () => {
    const graph = stateGraph('stateDiagram-v2\n  [*] --> On\n  state On {\n    [*] --> Idle\n    Idle --> Busy\n  }\n  On --> [*]');
    expect(graph.transitions.filter((t) => t.nested).map((t) => t.to)).toEqual(['Idle', 'Busy']);
    expect(initialState(graph)).toBe('On');
  });

  it.each([
    ['uml', stateSvg, STATE_SOURCE, 3, 5],
    ['auth', authStateSvg, AUTH_STATE_SOURCE, 3, 6],
  ])('finds the states and transitions of the %s diagram', (_, markup, source, states, transitions) => {
    const svg = load(markup);
    const parts = stateParts(svg, stateGraph(source));
    expect(parts.nodes.size).toBe(states);
    expect(parts.edges.size).toBe(transitions);
  });

  it('marks a run: current state, the transition taken, what was visited', () => {
    const svg = load(stateSvg);
    const parts = stateParts(svg, stateGraph(STATE_SOURCE));
    const [draft, review] = [parts.nodes.get('Draft')!, parts.nodes.get('InReview')!];
    markRun(svg, { current: [review], taken: parts.edges.get(1)!, visited: [draft] });

    expect(svg.hasAttribute('data-running')).toBe(true);
    expect(review.hasAttribute('data-current')).toBe(true);
    expect(draft.hasAttribute('data-visited')).toBe(true);
    expect(parts.edges.get(1)!.every((e) => e.hasAttribute('data-related'))).toBe(true);

    markRun(svg, null);
    expect(svg.querySelectorAll('[data-current], [data-visited], [data-related]')).toHaveLength(0);
  });

  it('marks focus on a tapped state and its transitions', () => {
    const svg = load(stateSvg);
    const parts = stateParts(svg, stateGraph(STATE_SOURCE));
    markFocus(svg, parts.edges.get(1)!, [parts.nodes.get('InReview')!]);
    expect(svg.hasAttribute('data-focus')).toBe(true);
    expect(parts.nodes.get('InReview')!.hasAttribute('data-selected')).toBe(true);
    markFocus(svg, []);
    expect(svg.hasAttribute('data-focus')).toBe(false);
  });
});

describe('activity diagrams', () => {
  const graph = activityGraph(ACTIVITY_SOURCE);

  it('reads node kinds, chains and labels', () => {
    expect(Object.fromEntries(graph.nodes)).toEqual({
      start: 'start',
      edit: 'action',
      online: 'decision',
      fork1: 'fork',
      queue: 'action',
      stop: 'end',
      save: 'action',
      send: 'action',
      join1: 'join',
    });
    expect(graph.edges.filter((e) => e.label).map((e) => e.label)).toEqual(['yes', 'no']);
    expect(activityGraph('flowchart TD\n  a --> b --> c').edges).toHaveLength(2);
    expect(activityGraph('flowchart TD\n  a -->|"yes"| b').edges[0].label).toBe('yes');
  });

  it('walks: next, a choice at the decision, fork branches together, join, end', () => {
    let step = firstStep(graph)!;
    expect(step.active).toEqual(['start']);
    step = nextStep(graph, step)!;
    step = nextStep(graph, step)!;
    expect(step.active).toEqual(['online']);
    expect(choicesAt(graph, step).map((e) => graph.edges[e].label)).toEqual(['yes', 'no']);
    expect(nextStep(graph, step)).toBeNull(); // a decision needs a choice

    step = nextStep(graph, step, choicesAt(graph, step)[0])!;
    expect(step.active).toEqual(['fork1']);
    step = nextStep(graph, step)!;
    expect(step.active).toEqual(['save', 'send']);
    step = nextStep(graph, step)!;
    expect(step.active).toEqual(['join1']);
    step = nextStep(graph, step)!;
    expect(step.active).toEqual(['stop']);
    expect(isDone(graph, step)).toBe(true);
  });

  it('finds every node and edge in the real SVG', () => {
    const parts = flowParts(load(activitySvg), graph);
    expect(parts.nodes.size).toBe(9);
    expect(parts.edges.size).toBe(graph.edges.length);
    expect(parts.edges.get(2)!).toHaveLength(2); // the "yes" edge has a label
  });
});
