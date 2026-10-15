import { describe, expect, it } from 'vitest';
import { splitByTerms } from './terms';

const term = (name: string) => ({ term: name, role: `${name} role`, anchor: 'entities' });

describe('splitByTerms', () => {
  it('matches whole identifiers only, longest name first', () => {
    const parts = splitByTerms(
      'AccessRequestStates s; AccessRequest r = AccessRequest.Submit(); x.AccessRequest',
      [term('AccessRequest'), term('AccessRequestStates')],
    );

    expect(parts.filter((p) => p.term).map((p) => p.text)).toEqual([
      'AccessRequestStates',
      'AccessRequest',
      'AccessRequest',
    ]);
    expect(parts.map((p) => p.text).join('')).toBe(
      'AccessRequestStates s; AccessRequest r = AccessRequest.Submit(); x.AccessRequest',
    );
  });

  it('leaves text alone without terms', () => {
    expect(splitByTerms('Result r', [])).toEqual([{ text: 'Result r' }]);
  });
});
