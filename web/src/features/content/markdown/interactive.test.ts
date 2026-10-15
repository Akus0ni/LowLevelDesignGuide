import { describe, expect, it } from 'vitest';
import classSvg from './__fixtures__/auth-class.svg?raw';
import sequenceNotesSvg from './__fixtures__/auth-sequence-notes.svg?raw';
import sequenceSvg from './__fixtures__/auth-sequence.svg?raw';
import stateSvg from './__fixtures__/auth-state.svg?raw';
import {
  analyzeClassDiagram,
  analyzeSequence,
  applyClassFocus,
  applySequenceStep,
  classNameOf,
  kindOf,
  makeClassesFocusable,
  neighboursOf,
} from './interactive';

/** The real Mermaid 12 output for the Auth lesson's diagrams (see __fixtures__). */
function load(markup: string): SVGSVGElement {
  const host = document.createElement('div');
  host.innerHTML = markup;
  return host.querySelector('svg')!;
}

describe('kindOf', () => {
  it('tells class, sequence and other diagrams apart', () => {
    expect(kindOf(load(classSvg))).toBe('class');
    expect(kindOf(load(sequenceSvg))).toBe('sequence');
    expect(kindOf(load(stateSvg))).toBe('other');
  });
});

describe('class diagrams', () => {
  it('finds every class and the two ends of every relationship', () => {
    const model = analyzeClassDiagram(load(classSvg));

    expect(model.nodes.size).toBe(21);
    expect([...model.nodes.keys()]).toContain('SessionManager');
    expect(model.edges).toHaveLength(17); // every relationship in the lesson, both ends recognised
    expect(neighboursOf(model, 'SessionManager')).toEqual([
      'IRefreshTokenRepository',
      'ITokenIssuer',
      'Login',
      'RefreshSession',
      'RefreshToken',
    ]);
    expect(neighboursOf(model, 'AccessRequest')).toEqual(['ApproveAccessRequest', 'IAccessRequestState']);
  });

  it('focuses a class: it and its relationships are marked, the rest is left to dim', () => {
    const svg = load(classSvg);
    const model = analyzeClassDiagram(svg);
    makeClassesFocusable(model);

    applyClassFocus(svg, model, 'AccessRequest');

    expect(svg.hasAttribute('data-focus')).toBe(true);
    const related = [...model.nodes]
      .filter(([, node]) => node.hasAttribute('data-related'))
      .map(([name]) => name);
    expect(related.sort()).toEqual(['AccessRequest', 'ApproveAccessRequest', 'IAccessRequestState']);
    expect(model.nodes.get('AccessRequest')!.getAttribute('aria-pressed')).toBe('true');
    expect(svg.querySelectorAll('path[data-related]')).toHaveLength(2);
    // The "delegates to" label travels with its relationship.
    expect(svg.querySelector('.edgeLabel[data-related]')?.textContent).toContain('delegates to');
  });

  it('clears the focus', () => {
    const svg = load(classSvg);
    const model = analyzeClassDiagram(svg);
    applyClassFocus(svg, model, 'Login');

    applyClassFocus(svg, model, null);

    expect(svg.hasAttribute('data-focus')).toBe(false);
    expect(svg.querySelectorAll('[data-related], [data-selected]')).toHaveLength(0);
  });

  it('makes classes keyboard buttons and maps a click inside a box to its class', () => {
    const svg = load(classSvg);
    const model = analyzeClassDiagram(svg);
    makeClassesFocusable(model);

    const login = model.nodes.get('Login')!;
    expect(login.getAttribute('role')).toBe('button');
    expect(login.getAttribute('tabindex')).toBe('0');
    expect(login.getAttribute('aria-label')).toBe('Class Login');
    expect(classNameOf(model, login.querySelector('path'))).toBe('Login');
    expect(classNameOf(model, svg)).toBeNull();
  });

  it('finds nothing in a diagram that is not a class diagram', () => {
    expect(analyzeClassDiagram(load(sequenceSvg)).nodes.size).toBe(0);
  });
});

describe('sequence diagrams', () => {
  it('turns the diagram into one step per message, in order, with readable text', () => {
    const steps = analyzeSequence(load(sequenceSvg));

    expect(steps).toHaveLength(15);
    expect(steps[0].text).toBe('Request access (email, name, note)');
    expect(steps[1].text).toBe('POST /api/access-requests');
    // Text, line and number of a message move together.
    expect(steps[0].elements.map((e) => e.getAttribute('class') ?? e.tagName)).toEqual([
      'messageText',
      'messageLine0',
      'line', // the numbered dot (a marker on a zero-length line)
      'sequenceNumber',
    ]);
  });

  it('includes notes as steps where they happen', () => {
    const steps = analyzeSequence(load(sequenceNotesSvg));

    expect(steps.filter((step) => step.text.startsWith('Note: '))).toHaveLength(2);
  });

  it('marks past, current and future steps, and shows everything again with null', () => {
    const svg = load(sequenceSvg);
    const steps = analyzeSequence(svg);

    applySequenceStep(svg, steps, 2);

    expect(svg.hasAttribute('data-stepping')).toBe(true);
    expect(steps.map((step) => step.elements[0].getAttribute('data-step')).slice(0, 4)).toEqual([
      'past',
      'past',
      'current',
      'future',
    ]);

    applySequenceStep(svg, steps, null);

    expect(svg.hasAttribute('data-stepping')).toBe(false);
    expect(svg.querySelectorAll('[data-step]')).toHaveLength(0);
  });
});
