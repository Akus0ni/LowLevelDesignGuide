import { useCallback, useEffect, useRef, useState } from 'react';
import { NoteMarkdown } from '../content/markdown/MarkdownView';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import type { Note } from '../../data/types';
import { deleteNote, editNote, isBlank, isLive, newNote, NOTE_LIMITS, type Place } from './noteRecords';
import { backIcon, trashIcon } from './noteIcons';
import { applyStarter, STARTERS } from './starters';
import { useNotesStore } from './useNotes';

/** Typing pauses this long before the note is saved (spec 007 §3.2). */
export const AUTOSAVE_MS = 800;

type Status = 'idle' | 'saving' | 'saved';

/**
 * Writes one note (spec 007 §3.2): a plain text box that saves itself 800 ms after typing stops and when it closes or
 * the page is hidden, an Edit / Preview switch, starters while it's empty, and Delete with Undo. A note emptied and
 * closed is deleted; an empty new note is never saved.
 */
export function NoteEditor({
  place,
  placeLabel,
  noteId,
  onDone,
  onDeleted,
}: {
  place: Place;
  placeLabel: string;
  /** The note to edit; a new note when it isn't in the store yet. */
  noteId: string;
  onDone: () => void;
  /** The note was deleted: the sheet offers Undo where the learner is (a toast would sit behind the modal sheet). */
  onDeleted: (deleted: Note) => void;
}) {
  const store = useNotesStore();
  const stored = store.getSnapshot().byId.get(noteId);
  const [body, setBody] = useState(stored && isLive(stored) ? stored.body : '');
  const [preview, setPreview] = useState(false);
  const [status, setStatus] = useState<Status>('idle');
  const field = useRef<HTMLTextAreaElement>(null);
  const pending = useRef<string | null>(null);
  /** What the box holds now, saved or not: decides whether closing deletes an emptied note. */
  const latest = useRef(body);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);

  const { lessonId, anchor } = place;

  /** Saves `text` over whatever the store holds now, so it never writes over a newer copy of the note. */
  const persist = useCallback(
    (text: string) => {
      const existing = store.getSnapshot().byId.get(noteId);
      const now = store.stamp();
      store.putNote(
        existing ? { ...editNote(existing, text, now), deletedAt: null } : newNote({ lessonId, anchor }, text, now, noteId),
      );
    },
    [store, noteId, lessonId, anchor],
  );

  /** Saves what is waiting, if anything; an empty text is never saved. Returns whether something was saved. */
  const flush = useCallback(() => {
    clearTimeout(timer.current);
    const text = pending.current;
    pending.current = null;
    if (text === null || isBlank(text)) return false;
    persist(text);
    return true;
  }, [persist]);

  const change = (text: string) => {
    setBody(text);
    pending.current = text;
    latest.current = text;
    setStatus(isBlank(text) ? 'idle' : 'saving');
    clearTimeout(timer.current);
    timer.current = setTimeout(() => {
      if (flush()) setStatus('saved');
    }, AUTOSAVE_MS);
  };

  // Closing the editor (or leaving the lesson) saves what was typed; a note emptied and closed is deleted.
  useEffect(() => {
    const saveWhenHidden = () => {
      if (document.visibilityState === 'hidden') flush();
    };
    document.addEventListener('visibilitychange', saveWhenHidden);
    return () => {
      document.removeEventListener('visibilitychange', saveWhenHidden);
      const emptied = isBlank(latest.current);
      flush();
      const existing = store.getSnapshot().byId.get(noteId);
      if (emptied && existing && isLive(existing)) store.putNote(deleteNote(existing, store.stamp()));
    };
  }, [store, noteId, flush]);

  const startFrom = (starter: (typeof STARTERS)[number]) => {
    const { body: text, cursor } = applyStarter(starter);
    // Set the field directly too, so the cursor lands before the next key press (React re-renders with the same text).
    if (field.current) {
      field.current.value = text;
      field.current.focus();
      field.current.setSelectionRange(cursor, cursor);
    }
    change(text);
  };

  const remove = () => {
    clearTimeout(timer.current);
    pending.current = null;
    const existing = store.getSnapshot().byId.get(noteId);
    if (existing && isLive(existing)) {
      const deleted = deleteNote(existing, store.stamp());
      store.putNote(deleted);
      onDeleted(deleted);
    }
    onDone();
  };

  return (
    <div className="note-editor">
      <div className="note-editor-bar">
        <button type="button" className="note-back" onClick={onDone}>
          <Icon d={backIcon} size={18} />
          All notes
        </button>
        <div className="note-switch" role="group" aria-label="Show">
          <button type="button" aria-pressed={!preview} onClick={() => setPreview(false)}>
            Edit
          </button>
          <button type="button" aria-pressed={preview} onClick={() => setPreview(true)}>
            Preview
          </button>
        </div>
      </div>
      <p className="note-stereotype">
        «my note» <span className="text-muted">· {placeLabel}</span>
      </p>
      {preview ? (
        <div className="note-preview">
          {isBlank(body) ? <p className="text-muted">Nothing to preview yet.</p> : <NoteMarkdown markdown={body} />}
        </div>
      ) : (
        <textarea
          ref={field}
          className="note-field"
          value={body}
          onChange={(event) => change(event.target.value)}
          maxLength={NOTE_LIMITS.maxBodyLength}
          aria-label={`Note on ${placeLabel}`}
          placeholder="Write in your own words. Markdown works: **bold**, lists, `code`, #tags."
          autoFocus={!stored}
          spellCheck
        />
      )}
      {isBlank(body) && !preview && (
        <div className="note-starters" role="group" aria-label="Start from">
          {STARTERS.map((starter) => (
            <button key={starter.id} type="button" className="note-starter" onClick={() => startFrom(starter)}>
              {starter.label}
            </button>
          ))}
        </div>
      )}
      <div className="note-editor-foot">
        <span className={cn('note-status', status === 'saved' && 'is-saved')} aria-live="polite">
          {status === 'saving' ? 'Saving…' : status === 'saved' ? 'Saved' : ''}
        </span>
        <button type="button" className="note-delete" onClick={remove}>
          <Icon d={trashIcon} size={16} />
          Delete
        </button>
      </div>
    </div>
  );
}
