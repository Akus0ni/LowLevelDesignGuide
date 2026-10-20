import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parseTags } from './tags';

interface TagCase {
  name: string;
  body: string;
  tags: string[];
}

/** The same vector Lld.Domain's TagParser is tested against (spec 007 A6). */
const cases = JSON.parse(
  readFileSync(resolve(process.cwd(), '..', 'tests', 'fixtures', 'notes', 'tags.json'), 'utf8'),
) as TagCase[];

describe('parseTags (shared vector)', () => {
  it.each(cases.map((c) => [c.name, c] as const))('%s', (_, tagCase) => {
    expect(parseTags(tagCase.body)).toEqual(tagCase.tags);
  });
});
