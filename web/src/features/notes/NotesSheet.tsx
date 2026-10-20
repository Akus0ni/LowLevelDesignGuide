import { Drawer } from '@base-ui/react/drawer';
import { useMemo, useState } from 'react';
import type { Note } from '../../data/types';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { DESKTOP, useMediaQuery } from '../../ui/useMediaQuery';
import { timeAgo } from '../progress/progressMath';
import { NoteEditor } from './NoteEditor';
import { plusIcon } from './noteIcons';
import { readableAnchor, sectionsOf, type LessonInfo } from './notesQuery';
import type { NotesFocus } from './sectionAnnotations';
import { excerpt } from './excerpt';
import { parseTags } from './tags';
import { useLessonNotes } from './useNotes';

export interface SheetSection {
  anchor: string;
  title: string;
}

type View = { kind: 'list' } | { kind: 'edit'; noteId: string; anchor: string };

const WHOLE_LESSON = 'Whole lesson';
const EXCERPT_CHARS = 180;


function viewFor(focus: NotesFocus | undefined): View {
  if (focus?.noteId) return { kind: 'edit', noteId: focus.noteId, anchor: focus.anchor ?? '' };
  if (focus?.create) return { kind: 'edit', noteId: crypto.randomUUID(), anchor: focus.anchor ?? '' };
  return { kind: 'list' };
}

/**
 * A lesson's notes (spec 007 §3.2): a bottom sheet on phones, a side panel on desktop, with the lesson still in place
 * underneath. The list is grouped Whole lesson, then sections in reading order; New note asks where, with the section
 * being read chosen already.
 */
export function NotesSheet({
  lessonId,
  lessonTitle,
  sections,
  currentAnchor,
  open,
  focus,
  onOpenChange,
}: {
  lessonId: string;
  lessonTitle: string;
  sections: SheetSection[];
  /** The h2 being read: preselected for a new note. */
  currentAnchor: string;
  open: boolean;
  focus?: NotesFocus;
  onOpenChange: (open: boolean) => void;
}) {
  const desktop = useMediaQuery(DESKTOP);
  return (
    <Drawer.Root open={open} onOpenChange={onOpenChange} swipeDirection={desktop ? 'right' : 'down'}>
      <Drawer.Portal>
        <Drawer.Backdrop className="sheet-backdrop" />
        <Drawer.Viewport className={cn('fixed inset-0 z-30 flex', desktop ? 'justify-end' : 'items-end justify-center')}>
          <Drawer.Popup className={cn('sheet notes-sheet', desktop && 'notes-panel')}>
            {!desktop && <div aria-hidden="true" className="mx-auto mt-2.5 mb-1 h-1 w-9 rounded-full bg-fg/15" />}
            <Drawer.Title className="px-5 pt-3 font-mono text-[12px] tracking-[0.08em] text-muted">
              notes · <span className="normal-case">{lessonTitle}</span>
            </Drawer.Title>
            <Drawer.Description className="sr-only">Your notes on this lesson, saved as you type.</Drawer.Description>
            {/* A new view per opening: the focus asked for decides where it starts. */}
            {open && (
              <SheetBody
                key={`${focus?.noteId ?? ''}|${focus?.anchor ?? ''}|${focus?.create ?? false}`}
                lessonId={lessonId}
                sections={sections}
                currentAnchor={currentAnchor}
                focus={focus}
              />
            )}
          </Drawer.Popup>
        </Drawer.Viewport>
      </Drawer.Portal>
    </Drawer.Root>
  );
}

function SheetBody({
  lessonId,
  sections,
  currentAnchor,
  focus,
}: {
  lessonId: string;
  sections: SheetSection[];
  currentAnchor: string;
  focus?: NotesFocus;
}) {
  const [view, setView] = useState<View>(() => viewFor(focus));
  const notes = useLessonNotes(lessonId);
  const titles = useMemo(() => new Map(sections.map((section) => [section.anchor, section.title])), [sections]);
  const placeLabel = (anchor: string) => (anchor ? (titles.get(anchor) ?? readableAnchor(anchor)) : WHOLE_LESSON);

  if (view.kind === 'edit') {
    return (
      <div className="notes-sheet-body">
        <NoteEditor
          key={view.noteId}
          place={{ lessonId, anchor: view.anchor }}
          placeLabel={placeLabel(view.anchor)}
          noteId={view.noteId}
          onDone={() => setView({ kind: 'list' })}
        />
      </div>
    );
  }

  return (
    <div className="notes-sheet-body">
      <NewNote
        currentAnchor={focus?.anchor ?? currentAnchor}
        currentTitle={titles.get(focus?.anchor ?? currentAnchor)}
        onCreate={(anchor) => setView({ kind: 'edit', noteId: crypto.randomUUID(), anchor })}
      />
      <NoteList
        notes={notes}
        lesson={{ id: lessonId, moduleSlug: '', slug: '', title: '', order: 0, sections }}
        focusAnchor={focus?.anchor}
        placeLabel={placeLabel}
        onOpen={(note) => setView({ kind: 'edit', noteId: note.id, anchor: note.anchor })}
      />
    </div>
  );
}

/** New note asks where (spec §3.2): the whole lesson, or the section being read (chosen already). */
function NewNote({
  currentAnchor,
  currentTitle,
  onCreate,
}: {
  currentAnchor: string;
  currentTitle?: string;
  onCreate: (anchor: string) => void;
}) {
  const [anchor, setAnchor] = useState(currentTitle ? currentAnchor : '');
  const places = currentTitle
    ? [
        { anchor: currentAnchor, label: currentTitle },
        { anchor: '', label: WHOLE_LESSON },
      ]
    : [{ anchor: '', label: WHOLE_LESSON }];
  return (
    <div className="note-new">
      {places.length > 1 && (
        <div className="note-places" role="radiogroup" aria-label="New note on">
          {places.map((place) => (
            <button
              key={place.anchor}
              type="button"
              role="radio"
              aria-checked={anchor === place.anchor}
              onClick={() => setAnchor(place.anchor)}
              className="note-place"
            >
              {place.label}
            </button>
          ))}
        </div>
      )}
      <button type="button" className="note-new-button" onClick={() => onCreate(anchor)}>
        <Icon d={plusIcon} size={18} />
        New note
      </button>
    </div>
  );
}

function NoteList({
  notes,
  lesson,
  focusAnchor,
  placeLabel,
  onOpen,
}: {
  notes: Note[];
  lesson: LessonInfo;
  focusAnchor?: string;
  placeLabel: (anchor: string) => string;
  onOpen: (note: Note) => void;
}) {
  const now = new Date();
  if (notes.length === 0) {
    return <p className="note-empty">No notes on this lesson yet. Your words here come back under its cheat sheet in Revise.</p>;
  }
  return (
    <div className="note-groups">
      {sectionsOf(notes, lesson).map((group) => (
        <section
          key={group.anchor || '(whole)'}
          aria-label={placeLabel(group.anchor)}
          className="note-group"
          data-focus={group.anchor === focusAnchor || undefined}
          ref={(element) => {
            if (element && group.anchor === focusAnchor && group.anchor) element.scrollIntoView({ block: 'nearest' });
          }}
        >
          <h3 className="note-group-title">{placeLabel(group.anchor)}</h3>
          <ul className="flex flex-col gap-2">
            {group.notes.map((note) => (
              <li key={note.id}>
                <button type="button" className="note-card" onClick={() => onOpen(note)}>
                  {group.missing.has(note.id) && <span className="note-orphan">section no longer exists</span>}
                  <span className="note-card-text">{excerpt(note.body, EXCERPT_CHARS)}</span>
                  <span className="note-card-meta">
                    {parseTags(note.body).map((tag) => (
                      <span key={tag} className="note-tag">
                        #{tag}
                      </span>
                    ))}
                    <span className="ml-auto">edited {timeAgo(note.updatedAt, now)}</span>
                  </span>
                </button>
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}
