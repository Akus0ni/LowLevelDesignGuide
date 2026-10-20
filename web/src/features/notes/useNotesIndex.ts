import { useLessons, useModuleDetails, useModules } from '../../data/useContent';
import type { LessonInfo, NotesIndex } from './notesQuery';

/**
 * Titles and order for the Notes tab and the export (spec 007 §3.4, §3.7): every module's lessons, plus the outline of
 * each lesson that has notes or bookmarks (`lessonIds`), for section titles. All local reads on the device. Suspends.
 */
export function useNotesIndex(lessonIds: readonly string[]): NotesIndex {
  const modules = useModules();
  const details = useModuleDetails(modules.map((module) => module.slug));
  const known = new Set(details.flatMap((module) => module.lessons.map((lesson) => lesson.id)));
  const withOutline = useLessons(lessonIds.filter((id) => known.has(id)));
  const outlines = new Map(
    withOutline.map((lesson) => [
      lesson.id,
      lesson.outline.filter((entry) => entry.level === 2).map((entry) => ({ anchor: entry.anchor, title: entry.text })),
    ]),
  );
  const lessons = new Map<string, LessonInfo>();
  details.forEach((module) =>
    module.lessons.forEach((lesson) =>
      lessons.set(lesson.id, {
        id: lesson.id,
        moduleSlug: module.slug,
        slug: lesson.slug,
        title: lesson.title,
        order: lesson.order,
        sections: outlines.get(lesson.id),
      }),
    ),
  );
  return { modules: modules.map(({ slug, title, order }) => ({ slug, title, order })), lessons };
}
