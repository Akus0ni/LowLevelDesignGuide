/** What the compare view shows of a pattern lesson (spec 006 §3.7), taken from the body the reader already loads. */
export interface PatternFacts {
  /** The first paragraph under `## The idea` (Markdown). */
  idea: string;
  /** The Mermaid source of the first diagram under `## Structure`, if any. */
  structure: string | null;
}

/**
 * Finds a lesson's idea and structure diagram by their headings, the same headings the packer requires of every
 * `pattern` lesson (`LessonSections` in Lld.Content.Authoring), so nothing is copied into the module.
 */
export function patternFacts(markdown: string): PatternFacts {
  const idea = sectionOf(markdown, 'The idea');
  const structure = sectionOf(markdown, 'Structure');
  return {
    idea: idea.split(/\n\s*\n/).map((block) => block.trim()).find((block) => block.length > 0) ?? '',
    structure: /```mermaid[^\n]*\n([\s\S]*?)\n```/.exec(structure)?.[1] ?? null,
  };
}

function sectionOf(markdown: string, heading: string): string {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  const start = lines.findIndex((line) => line.trim().toLowerCase() === `## ${heading.toLowerCase()}`);
  if (start < 0) return '';
  let fenced = false;
  const body: string[] = [];
  for (const line of lines.slice(start + 1)) {
    if (line.startsWith('```')) fenced = !fenced;
    if (!fenced && /^## /.test(line)) break;
    body.push(line);
  }
  return body.join('\n');
}
