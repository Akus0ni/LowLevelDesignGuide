/** A gap the learner fills in; the cursor goes to the first one. */
const BLANK = ' ';

export interface Starter {
  id: 'work' | 'interview' | 'gotcha';
  label: string;
  tag: string;
  prompts: string[];
}

/** Optional outlines that nudge learners towards notes that help in an interview (spec 007 §3.2). */
export const STARTERS: readonly Starter[] = [
  { id: 'work', label: 'My example from work', tag: 'work', prompts: ['Where', 'What it solved', "What I'd change"] },
  { id: 'interview', label: 'Interview answer (STAR)', tag: 'interview', prompts: ['Situation', 'Task', 'Action', 'Result'] },
  { id: 'gotcha', label: 'Gotcha I hit', tag: 'gotcha', prompts: ['Symptom', 'Cause', 'Fix'] },
];

/** The starter's text, and where the cursor goes: just after the first prompt. */
export function applyStarter(starter: Starter): { body: string; cursor: number } {
  const lines = starter.prompts.map((prompt) => `**${prompt}:**${BLANK}`);
  const body = `#${starter.tag}\n\n${lines.join('\n')}\n`;
  return { body, cursor: body.indexOf(lines[0]) + lines[0].length };
}
