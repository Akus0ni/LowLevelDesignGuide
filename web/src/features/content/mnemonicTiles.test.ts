import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { isPhrase, mnemonicLetters } from './mnemonicTiles';

interface MnemonicCase {
  name: string;
  word: string;
  letters: string[];
}

// The same file the .NET tests read (MnemonicsTests), so the packer and the app spell mnemonics alike.
const vector = JSON.parse(
  readFileSync(resolve(process.cwd(), '..', 'tests', 'fixtures', 'content', 'mnemonics.json'), 'utf8'),
) as { cases: MnemonicCase[] };

describe('mnemonicLetters (shared test vector)', () => {
  it.each(vector.cases.map((c) => [c.name, c] as const))('%s', (_, testCase) => {
    expect(mnemonicLetters(testCase.word).map((l) => l.letter)).toEqual(testCase.letters);
  });
});

describe('mnemonicLetters', () => {
  it('keeps the rest of each word of a phrase, without trailing punctuation', () => {
    expect(mnemonicLetters("Cats Can Ignore Instructions; Don't")).toEqual([
      { letter: 'C', rest: 'ats' },
      { letter: 'C', rest: 'an' },
      { letter: 'I', rest: 'gnore' },
      { letter: 'I', rest: 'nstructions' },
      { letter: 'D', rest: "on't" },
    ]);
  });

  it('tells a phrase from a word', () => {
    expect(isPhrase('Smart Builders')).toBe(true);
    expect(isPhrase('A-PIE')).toBe(false);
  });
});
