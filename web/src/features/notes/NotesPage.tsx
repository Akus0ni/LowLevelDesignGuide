import { useState } from 'react';
import { Link } from 'react-router';
import type { Bookmark, Note } from '../../data/types';
import { useDataSource } from '../../data/DataSourceContext';
import { Icon } from '../../ui/Icon';
import { ContentBoundary } from '../content/ContentBoundary';
import { ListSkeleton } from '../content/Skeletons';
import { timeAgo } from '../progress/progressMath';
import { buildExport, exportFileName } from './exportNotes';
import { deleteNote, isLive, restoreNote } from './noteRecords';
import { downloadIcon, ribbonIcon } from './noteIcons';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];
import { groupBookmarks, groupNotes, readableAnchor, tagCounts, type LessonInfo, type NotesFilter } from './notesQuery';
import { excerpt } from './excerpt';
import { parseTags } from './tags';
import { undoToast } from './undoToast';
import { useNotes, useNotesStore } from './useNotes';
import { useNotesIndex } from './useNotesIndex';

type Tab = 'notes' | 'bookmarks';

const TAB_KEY = 'lld.notes.tab';
const FILTER_KEY = 'lld.notes.filter';
const EXCERPT_CHARS = 160;

/** Remembered for the session only (spec §3.4); storage may be missing, which is fine. */
function sessionValue<T>(key: string, fallback: T): T {
  try {
    const raw = sessionStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback;
  }
}

function remember(key: string, value: unknown): void {
  try {
    sessionStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Not remembered: the default comes back next time.
  }
}


const lessonPath = (lesson: LessonInfo) => `/learn/${lesson.moduleSlug}/${lesson.slug}`;

/** The Notes tab (spec 007 §3.4): every note and bookmark, grouped by module and lesson, one tap from where it belongs. */
export function NotesPage() {
  const [tab, setTab] = useState<Tab>(() => sessionValue<Tab>(TAB_KEY, 'notes'));
  const choose = (next: Tab) => {
    setTab(next);
    remember(TAB_KEY, next);
  };
  return (
    <section className="pt-8 md:pt-12">
      <title>Notes · LLD Guide</title>
      <h1 className="text-[34px] font-semibold md:text-5xl">Notes</h1>
      <p className="mt-2 max-w-prose text-muted">Your notes and bookmarks, saved on this device.</p>
      <div className="notes-tabs" role="tablist" aria-label="Show">
        {(['notes', 'bookmarks'] as const).map((value) => (
          <button
            key={value}
            type="button"
            role="tab"
            id={`notes-tab-${value}`}
            aria-selected={tab === value}
            aria-controls={`notes-panel-${value}`}
            onClick={() => choose(value)}
            className="notes-tab"
          >
            {value === 'notes' ? 'Notes' : 'Bookmarks'}
          </button>
        ))}
      </div>
      <div role="tabpanel" id={`notes-panel-${tab}`} aria-labelledby={`notes-tab-${tab}`}>
        <ContentBoundary fallback={<ListSkeleton />}>{tab === 'notes' ? <NotesList /> : <BookmarksList />}</ContentBoundary>
      </div>
    </section>
  );
}

function NotesList() {
  const { byId, bookmarks } = useNotes();
  const store = useNotesStore();
  const source = useDataSource();
  const notes = [...byId.values()].filter(isLive);
  const index = useNotesIndex([...new Set([...notes.map((note) => note.lessonId), ...[...bookmarks.values()].map((b) => b.lessonId)])]);
  const [filter, setFilter] = useState<NotesFilter>(() => sessionValue<NotesFilter>(FILTER_KEY, { tags: [], text: '' }));
  const [exporting, setExporting] = useState(false);
  const update = (next: NotesFilter) => {
    setFilter(next);
    remember(FILTER_KEY, next);
  };

  if (notes.length === 0) {
    return (
      <p className="notes-empty">
        No notes yet. Open a lesson and use <strong>Add note here</strong> on any section.
      </p>
    );
  }

  const tags = tagCounts(notes);
  const groups = groupNotes(notes, index, filter);
  const toggleTag = (tag: string) =>
    update({ ...filter, tags: filter.tags.includes(tag) ? filter.tags.filter((t) => t !== tag) : [...filter.tags, tag] });
  const remove = (note: Note) => {
    const deleted = deleteNote(note, store.stamp());
    store.putNote(deleted);
    undoToast('Note deleted', () => store.putNote(restoreNote(deleted, store.stamp())));
  };
  const exportAll = async () => {
    setExporting(true);
    try {
      await source.exportNotes(exportFileName(new Date()), buildExport(notes, index));
    } finally {
      setExporting(false);
    }
  };

  return (
    <>
      <div className="notes-filters">
        <input
          type="search"
          value={filter.text}
          onChange={(event) => update({ ...filter, text: event.target.value })}
          placeholder="Filter notes"
          aria-label="Filter notes"
          enterKeyHint="search"
          className="notes-filter-text"
        />
        <button type="button" className="notes-export" onClick={() => void exportAll()} disabled={exporting}>
          <Icon d={downloadIcon} size={18} />
          {exporting ? 'Preparing…' : 'Export notes'}
        </button>
      </div>
      {tags.length > 0 && (
        <div className="notes-tag-filter" role="group" aria-label="Tags">
          {tags.map(({ tag, count }) => (
            <button
              key={tag}
              type="button"
              aria-pressed={filter.tags.includes(tag)}
              onClick={() => toggleTag(tag)}
              className="notes-tag-chip"
            >
              #{tag} <span className="tabular-nums opacity-70">{count}</span>
            </button>
          ))}
        </div>
      )}
      {groups.length === 0 && <p className="notes-empty">No notes match. Clear a tag or the filter to see more.</p>}
      {groups.map(({ module, lessons }) => (
        <section key={module?.slug ?? '(removed)'} className="notes-module" aria-label={module?.title ?? 'Removed lessons'}>
          <h2 className="notes-module-title">{module?.title ?? 'Removed lessons'}</h2>
          {lessons.map(({ lessonId, lesson, sections }) => (
            <div key={lessonId} className="notes-lesson">
              <h3 className="notes-lesson-title">{lesson?.title ?? lessonId}</h3>
              <ul className="flex flex-col gap-2">
                {sections.flatMap((group) =>
                  group.notes.map((note) => {
                    const place = group.title ?? readableAnchor(group.anchor);
                    const body = (
                      <>
                        <span className="note-card-place">
                          {place}
                          {group.missing.has(note.id) && <span className="note-orphan"> · section no longer exists</span>}
                        </span>
                        <span className="note-card-text">{excerpt(note.body, EXCERPT_CHARS)}</span>
                        <span className="note-card-meta">
                          {parseTags(note.body).map((tag) => (
                            <span key={tag} className="note-tag">
                              #{tag}
                            </span>
                          ))}
                          <span className="ml-auto">edited {timeAgo(note.updatedAt, new Date())}</span>
                        </span>
                      </>
                    );
                    return (
                      <li key={note.id}>
                        {lesson ? (
                          <Link
                            to={`${lessonPath(lesson)}?${new URLSearchParams({ ...(note.anchor && { at: note.anchor }), note: note.id })}`}
                            className="note-card"
                          >
                            {body}
                          </Link>
                        ) : (
                          <div className="note-card">
                            {body}
                            <button type="button" className="note-delete self-start" onClick={() => remove(note)}>
                              Delete
                            </button>
                          </div>
                        )}
                      </li>
                    );
                  }),
                )}
              </ul>
            </div>
          ))}
        </section>
      ))}
    </>
  );
}

const KIND_LABEL: Record<Bookmark['kind'], string> = { lesson: 'Lesson', section: 'Section', cheatSheet: 'Cheat sheet' };

function BookmarksList() {
  const { bookmarks } = useNotes();
  const store = useNotesStore();
  const live = [...bookmarks.values()].filter(isLive);
  const index = useNotesIndex([...new Set(live.map((bookmark) => bookmark.lessonId))]);

  if (live.length === 0) {
    return <p className="notes-empty">Nothing bookmarked yet. Use the ribbon on a lesson, a section or a cheat sheet.</p>;
  }

  const remove = (bookmark: Bookmark) => {
    store.toggleBookmark(bookmark, false);
    undoToast('Bookmark removed', () => store.toggleBookmark(bookmark, true));
  };
  const target = (bookmark: Bookmark, lesson: LessonInfo) =>
    bookmark.kind === 'cheatSheet'
      ? `/revise/${lesson.moduleSlug}/${lesson.slug}`
      : bookmark.kind === 'section'
        ? `${lessonPath(lesson)}?${new URLSearchParams({ at: bookmark.anchor })}`
        : lessonPath(lesson);
  const label = (bookmark: Bookmark, lesson: LessonInfo | null) =>
    bookmark.kind === 'section'
      ? (lesson?.sections?.find((section) => section.anchor === bookmark.anchor)?.title ?? readableAnchor(bookmark.anchor))
      : (lesson?.title ?? bookmark.lessonId);

  return groupBookmarks(live, index).map(({ module, lessons }) => (
    <section key={module?.slug ?? '(removed)'} className="notes-module" aria-label={module?.title ?? 'Removed lessons'}>
      <h2 className="notes-module-title">{module?.title ?? 'Removed lessons'}</h2>
      <ul className="flex flex-col gap-2">
        {lessons.flatMap(({ lesson, bookmarks: items }) =>
          items.map((bookmark) => (
            <li key={`${bookmark.kind}|${bookmark.lessonId}|${bookmark.anchor}`} className="bookmark-row">
              {lesson ? (
                <Link to={target(bookmark, lesson)} className="bookmark-link">
                  <BookmarkText bookmark={bookmark} lesson={lesson} label={label(bookmark, lesson)} />
                </Link>
              ) : (
                <span className="bookmark-link">
                  <BookmarkText bookmark={bookmark} lesson={null} label={label(bookmark, null)} />
                </span>
              )}
              <button
                type="button"
                className="bookmark-remove"
                onClick={() => remove(bookmark)}
                aria-label={`Remove bookmark: ${label(bookmark, lesson)}`}
              >
                Remove
              </button>
            </li>
          )),
        )}
      </ul>
    </section>
  ));
}

function BookmarkText({ bookmark, lesson, label }: { bookmark: Bookmark; lesson: LessonInfo | null; label: string }) {
  return (
    <>
      <span className="bookmark-kind" aria-hidden="true">
        <Icon d={bookmark.kind === 'cheatSheet' ? boltIcon : ribbonIcon} size={16} />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block font-medium">{label}</span>
        <span className="block text-[13px] text-muted">
          {KIND_LABEL[bookmark.kind]}
          {bookmark.kind !== 'lesson' && lesson ? ` · ${lesson.title}` : ''}
        </span>
      </span>
    </>
  );
}
