import type { ModulePicker, PickerAnswer, PickerQuestion } from '../../../data/types';

/** Where a walk through the picker stands: the next question to ask, or the answer that picked lessons. */
export type PickerStep =
  | { kind: 'question'; question: PickerQuestion }
  | { kind: 'result'; answer: PickerAnswer };

/** One answer given so far: what was asked and what the learner chose. */
export interface PickerChoice {
  question: PickerQuestion;
  answer: PickerAnswer;
}

/**
 * Replays the answers chosen so far (indexes into each question's answers) from the start question. The packer
 * validated the tree (every `next` exists, no loops), so a walk always ends at a question or a result.
 */
export function walkPicker(picker: ModulePicker, chosen: readonly number[]): { step: PickerStep; path: PickerChoice[] } {
  const byId = new Map(picker.questions.map((question) => [question.id, question]));
  const path: PickerChoice[] = [];
  let question = byId.get(picker.start);
  for (const index of chosen) {
    const answer = question?.answers[index];
    if (!question || !answer) break;
    path.push({ question, answer });
    if (answer.picks.length > 0) return { step: { kind: 'result', answer }, path };
    question = answer.next ? byId.get(answer.next) : undefined;
  }
  if (!question) throw new Error(`The picker has no question '${picker.start}'.`);
  return { step: { kind: 'question', question }, path };
}
