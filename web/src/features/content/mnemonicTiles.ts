/** One letter of a mnemonic, and the rest of its word when the mnemonic is a phrase ("S" + "mart"). */
export interface MnemonicLetter {
  letter: string;
  rest: string;
}

const LETTER = /\p{L}/u;
const TRAILING_NON_LETTERS = /[^\p{L}]+$/u;

/**
 * How a mnemonic spells its lessons (spec 006 §3.4), the same rule as `Mnemonics.Letters` in Lld.Content.Authoring:
 * a phrase ("Smart Builders Always Follow Plans") uses the first letter of each word; a single word ("SOLID", "A-PIE")
 * uses every letter. Both are checked against `tests/fixtures/content/mnemonics.json`.
 */
export function mnemonicLetters(word: string): MnemonicLetter[] {
  const words = word.split(/\s+/).filter(Boolean);
  if (words.length <= 1) return [...word].filter((c) => LETTER.test(c)).map((letter) => ({ letter, rest: '' }));

  return words.flatMap((part) => {
    const index = [...part].findIndex((c) => LETTER.test(c));
    if (index < 0) return [];
    const chars = [...part];
    return [{ letter: chars[index], rest: chars.slice(index + 1).join('').replace(TRAILING_NON_LETTERS, '') }];
  });
}

/** True when the mnemonic is a phrase, drawn with each word's first letter big and the rest small. */
export function isPhrase(word: string): boolean {
  return word.trim().split(/\s+/).length > 1;
}
