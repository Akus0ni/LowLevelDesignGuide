import type { Bookmark, Note } from '../../data/types';
import { isLive } from './noteRecords';
import { parseTags } from './tags';

/** What the Notes tab and the export know about the content: titles and order (from the content, never stored). */
export interface ModuleInfo {
  slug: string;
  title: string;
  order: number;
}

export interface LessonInfo {
  id: string;
  moduleSlug: string;
  slug: string;
  title: string;
  order: number;
  /** The lesson's `h2` sections in reading order; undefined until the lesson's outline has been loaded. */
  sections?: { anchor: string; title: string }[];
}

export interface NotesIndex {
  modules: ModuleInfo[];
  lessons: Map<string, LessonInfo>;
}

export interface NotesFilter {
  /** Every selected tag must be on the note (AND). */
  tags: string[];
  /** Plain "contains", ignoring case; full search is spec 009. */
  text: string;
}

export const NO_FILTER: NotesFilter = { tags: [], text: '' };

/** A section's notes, or the whole lesson's (`anchor` ''). */
export interface SectionGroup {
  anchor: string;
  /** The section's heading; null for the whole lesson, or while the lesson's outline is loading. */
  title: string | null;
  notes: Note[];
  /** Notes here whose section is no longer in the lesson (spec §3.2): shown with the whole lesson, labelled. */
  missing: ReadonlySet<string>;
}

export interface LessonGroup {
  lessonId: string;
  lesson: LessonInfo | null;
  sections: SectionGroup[];
  lastEdit: string;
}

export interface ModuleGroup {
  /** null: notes on lessons no longer in the content ("Removed lessons"). */
  module: ModuleInfo | null;
  lessons: LessonGroup[];
}

// Filters as small composable predicates (Specification, spec 007 §6).

type NotePredicate = (note: Note) => boolean;

const hasTags =
  (tags: readonly string[]): NotePredicate =>
  (note) => {
    const own = parseTags(note.body);
    return tags.every((tag) => own.includes(tag));
  };

const contains =
  (text: string): NotePredicate =>
  (note) =>
    note.body.toLowerCase().includes(text.trim().toLowerCase());

const both =
  (...predicates: NotePredicate[]): NotePredicate =>
  (note) =>
    predicates.every((predicate) => predicate(note));

export const matchesFilter = (filter: NotesFilter): NotePredicate => both(isLive, hasTags(filter.tags), contains(filter.text));

/** Every tag on a live note, with how many notes carry it, most used first. */
export function tagCounts(notes: readonly Note[]): { tag: string; count: number }[] {
  const counts = new Map<string, number>();
  notes.filter(isLive).forEach((note) => parseTags(note.body).forEach((tag) => counts.set(tag, (counts.get(tag) ?? 0) + 1)));
  return [...counts].map(([tag, count]) => ({ tag, count })).sort((a, b) => b.count - a.count || a.tag.localeCompare(b.tag));
}

const NONE: ReadonlySet<string> = new Set();

/**
 * A lesson's notes in reading order: the whole lesson first, then each section in outline order; notes whose section
 * is gone join the whole lesson. Within a group, oldest first. Before the outline loads, groups follow the notes.
 */
export function sectionsOf(notes: readonly Note[], lesson: LessonInfo | null): SectionGroup[] {
  const sorted = [...notes].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  const outline = lesson?.sections;
  if (!outline) {
    const anchors = [...new Set(['', ...sorted.map((note) => note.anchor)])];
    return anchors
      .map((anchor) => ({ anchor, title: null, notes: sorted.filter((note) => note.anchor === anchor), missing: NONE }))
      .filter((group) => group.notes.length > 0);
  }
  const known = new Set(outline.map((section) => section.anchor));
  const whole = sorted.filter((note) => !known.has(note.anchor));
  const missing = new Set(whole.filter((note) => note.anchor !== '').map((note) => note.id));
  return [
    { anchor: '', title: null, notes: whole, missing },
    ...outline.map((section) => ({
      anchor: section.anchor,
      title: section.title,
      notes: sorted.filter((note) => note.anchor === section.anchor),
      missing: NONE,
    })),
  ].filter((group) => group.notes.length > 0);
}

/** Notes tab order (spec §3.4): modules in course order, then lessons with the latest edit first; removed lessons last. */
export function groupNotes(notes: readonly Note[], index: NotesIndex, filter: NotesFilter = NO_FILTER): ModuleGroup[] {
  return groupByModule(notes.filter(matchesFilter(filter)), index, (a, b) => b.lastEdit.localeCompare(a.lastEdit));
}

/** Export order (spec §3.7): modules and lessons in course order; removed lessons last. */
export function groupForExport(notes: readonly Note[], index: NotesIndex): ModuleGroup[] {
  return groupByModule(notes.filter(isLive), index, (a, b) => (a.lesson?.order ?? 0) - (b.lesson?.order ?? 0));
}

function groupByModule(
  notes: readonly Note[],
  index: NotesIndex,
  lessonOrder: (a: LessonGroup, b: LessonGroup) => number,
): ModuleGroup[] {
  const byLesson = new Map<string, Note[]>();
  notes.forEach((note) => byLesson.set(note.lessonId, [...(byLesson.get(note.lessonId) ?? []), note]));
  const lessons: LessonGroup[] = [...byLesson].map(([lessonId, lessonNotes]) => {
    const lesson = index.lessons.get(lessonId) ?? null;
    return {
      lessonId,
      lesson,
      sections: sectionsOf(lessonNotes, lesson),
      lastEdit: lessonNotes.reduce((latest, note) => (note.updatedAt > latest ? note.updatedAt : latest), ''),
    };
  });
  const modules = [...index.modules].sort((a, b) => a.order - b.order);
  const groups: ModuleGroup[] = modules
    .map((module) => ({
      module,
      lessons: lessons.filter((group) => group.lesson?.moduleSlug === module.slug).sort(lessonOrder),
    }))
    .filter((group) => group.lessons.length > 0);
  const removed = lessons.filter((group) => !group.lesson || !modules.some((m) => m.slug === group.lesson?.moduleSlug));
  if (removed.length > 0) groups.push({ module: null, lessons: removed.sort((a, b) => b.lastEdit.localeCompare(a.lastEdit)) });
  return groups;
}

export interface BookmarkGroup {
  module: ModuleInfo | null;
  lessons: { lessonId: string; lesson: LessonInfo | null; bookmarks: Bookmark[] }[];
}

const KIND_ORDER: Record<Bookmark['kind'], number> = { lesson: 0, section: 1, cheatSheet: 2 };

/** Bookmarks grouped like notes: modules and lessons in course order; within a lesson, lesson, sections, cheat sheet. */
export function groupBookmarks(bookmarks: readonly Bookmark[], index: NotesIndex): BookmarkGroup[] {
  const live = bookmarks.filter(isLive);
  const byLesson = new Map<string, Bookmark[]>();
  live.forEach((bookmark) => byLesson.set(bookmark.lessonId, [...(byLesson.get(bookmark.lessonId) ?? []), bookmark]));
  const sectionOrder = (lesson: LessonInfo | null, anchor: string) => {
    const at = lesson?.sections?.findIndex((section) => section.anchor === anchor) ?? -1;
    return at < 0 ? Number.MAX_SAFE_INTEGER : at;
  };
  const lessons = [...byLesson].map(([lessonId, items]) => {
    const lesson = index.lessons.get(lessonId) ?? null;
    const sorted = [...items].sort(
      (a, b) => KIND_ORDER[a.kind] - KIND_ORDER[b.kind] || sectionOrder(lesson, a.anchor) - sectionOrder(lesson, b.anchor),
    );
    return { lessonId, lesson, bookmarks: sorted };
  });
  const modules = [...index.modules].sort((a, b) => a.order - b.order);
  const groups: BookmarkGroup[] = modules
    .map((module) => ({
      module,
      lessons: lessons.filter((l) => l.lesson?.moduleSlug === module.slug).sort((a, b) => (a.lesson?.order ?? 0) - (b.lesson?.order ?? 0)),
    }))
    .filter((group) => group.lessons.length > 0);
  const removed = lessons.filter((l) => !l.lesson || !modules.some((m) => m.slug === l.lesson?.moduleSlug));
  if (removed.length > 0) groups.push({ module: null, lessons: removed });
  return groups;
}

/** "the-rule" → "The rule": a readable stand-in until the lesson's outline has loaded. */
export const readableAnchor = (anchor: string): string =>
  anchor ? anchor.charAt(0).toUpperCase() + anchor.slice(1).split('-').join(' ') : 'Whole lesson';
