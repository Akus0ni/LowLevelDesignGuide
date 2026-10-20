import { createContext, useContext } from 'react';

/** What the notes sheet should show when it opens. */
export interface NotesFocus {
  /** Section to start at ('' = whole lesson). */
  anchor?: string;
  /** A note to open in the editor. */
  noteId?: string;
  /** Start a new note at `anchor`. */
  create?: boolean;
}

/**
 * Lets lesson headings carry the learner's marks and actions (spec 007 §3.2). Only the lesson page provides it, so a
 * cheat sheet's headings, and headings inside a note's own preview, stay plain.
 */
export interface SectionAnnotations {
  lessonId: string;
  /** Section titles by heading id (the lesson's h2 outline). */
  sectionTitles: ReadonlyMap<string, string>;
  openNotes: (focus?: NotesFocus) => void;
}

export const SectionAnnotationsContext = createContext<SectionAnnotations | null>(null);

export const useSectionAnnotations = () => useContext(SectionAnnotationsContext);
