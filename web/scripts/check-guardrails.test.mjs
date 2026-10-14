import { describe, expect, it } from 'vitest';
import { findViolations } from './check-guardrails.mjs';

const rulesFor = (text) => findViolations(text).map((v) => v.rule);

describe('design guardrails', () => {
  it.each([
    ['className="transition-all duration-200"', 'no-transition-all'],
    ['className="h-screen"', 'no-h-screen'],
    ['className="min-h-screen"', 'no-h-screen'],
    ['className="hover:ease-in"', 'no-ease-in'],
    ['<meta name="viewport" content="width=device-width, user-scalable=no">', 'no-zoom-lock'],
    ['<meta name="viewport" content="maximum-scale=1">', 'no-zoom-lock'],
  ])('flags %s', (text, rule) => {
    expect(rulesFor(text)).toContain(rule);
  });

  it.each([
    'className="transition-[transform,opacity] ease-out"',
    'className="h-dvh min-h-dvh"',
    'className="ease-in-out"',
    '--ease-in: initial;',
    '<meta name="viewport" content="width=device-width, initial-scale=1">',
  ])('allows %s', (text) => {
    expect(rulesFor(text)).toEqual([]);
  });
});
