/** A fenced block: an opening fence of 3+ backticks or tildes up to the matching close (or the end of the note). */
const FENCED_CODE = /^ {0,3}(`{3,}|~{3,})[^\n]*\n[\s\S]*?(?:^ {0,3}\1[ \t]*$|(?![\s\S]))/gm;
/** A code span: a run of backticks up to the same run. */
const INLINE_CODE = /(`+)[\s\S]*?\1/g;
const TAG = /(?<![A-Za-z0-9_/#&])#([A-Za-z][A-Za-z0-9-]{0,29})(?![A-Za-z0-9_-])/g;

/**
 * The `#tags` a learner wrote in a note (spec 007 §3.5): `#`, a letter, then up to 29 letters, digits or hyphens. Not
 * tags: anything in code, Markdown headings (`# Title`), URL fragments, HTML entities and `##`. Lower case, without
 * repeats, in the order they first appear. Lld.Domain's `TagParser` follows the same rule; both are checked against
 * `tests/fixtures/notes/tags.json`.
 */
export function parseTags(body: string): string[] {
  const text = body.replace(/\r\n?/g, '\n').replace(FENCED_CODE, ' ').replace(INLINE_CODE, ' ');
  return [...new Set([...text.matchAll(TAG)].map((match) => match[1].toLowerCase()))];
}
