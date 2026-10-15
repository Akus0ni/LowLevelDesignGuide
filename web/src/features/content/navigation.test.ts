import { describe, expect, it } from 'vitest';
import { formatMinutes, plural } from './navigation';

describe('formatMinutes', () => {
  it.each([
    [1, '1 min'],
    [59, '59 min'],
    [60, '1 h'],
    [120, '2 h'],
    [939, '15 h 39 min'],
  ])('%i minutes reads "%s"', (minutes, text) => {
    expect(formatMinutes(minutes)).toBe(text);
  });
});

describe('plural', () => {
  it('uses the singular for exactly one', () => {
    expect(plural(1, 'lesson')).toBe('1 lesson');
    expect(plural(0, 'lesson')).toBe('0 lessons');
    expect(plural(2, 'cheat sheet')).toBe('2 cheat sheets');
  });
});
