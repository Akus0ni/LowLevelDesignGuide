import { useMemo, useRef, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { useDataSource } from '../../data/DataSourceContext';
import type { LessonDetail, ModuleDetail, OutlineEntry } from '../../data/types';
import { content, preloadContent, useLesson, useModule } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { BookmarkToggle } from '../notes/BookmarkToggle';
import { pencilIcon } from '../notes/noteIcons';
import { NotesSheet } from '../notes/NotesSheet';
import { SectionAnnotationsContext, type NotesFocus } from '../notes/sectionAnnotations';
import { useLessonNotes, useSectionMarks } from '../notes/useNotes';
import { DoneControl } from '../progress/DoneControl';
import { MatchBar } from '../search/marks/MatchBar';
import { useSearchMarks } from '../search/marks/useSearchMarks';
import { useModuleCompleteToast } from '../progress/moduleCelebration';
import { Icon } from '../../ui/Icon';
import { BackLink } from './BackLink';
import { LessonIcon } from './LessonIcon';
import { ContentBoundary } from './ContentBoundary';
import { MarkdownView } from './markdown/MarkdownView';
import { LookalikeContext } from './patterns/lookalikeContext';
import { formatMinutes } from './navigation';
import { OutlineColumn, OutlineSheet } from './Outline';
import { NextUp } from './reader/NextUp';
import { TextSizeControl } from './reader/TextSizeControl';
import { useReadingTracker } from './reader/useReadingTracker';
import { useTextSize } from './reader/useTextSize';
import { LessonSkeleton } from './Skeletons';
import { useAnchorScroll } from './useAnchorScroll';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];

/** The reader: a lesson's text, its outline, its cheat sheet one tap away, and what to read next. */
export function LessonPage() {
  const { module = '', lesson = '' } = useParams();
  return (
    <div className="pt-4 md:pt-8">
      <ContentBoundary fallback={<LessonSkeleton />}>
        <LessonView moduleSlug={module} lessonSlug={lesson} />
      </ContentBoundary>
    </div>
  );
}

function LessonView({ moduleSlug, lessonSlug }: { moduleSlug: string; lessonSlug: string }) {
  const module = useModule(moduleSlug);
  const position = module.lessons.findIndex((l) => l.slug === lessonSlug);
  // An unknown slug asks for an id that can't exist, so the boundary shows "not found" like any missing lesson.
  const lesson = useLesson(module.lessons[position]?.id ?? `missing:${lessonSlug}`);
  const source = useDataSource();
  const textSize = useTextSize();
  const reading = useReadingTracker(lesson.id, lesson.outline);
  const notes = useLessonNotesSheet(lesson);
  const marks = useSectionMarks(lesson.id);
  useModuleCompleteToast(module);
  useAnchorScroll();
  const body = useRef<HTMLDivElement>(null);
  const matches = useSearchMarks(body, lesson.id, 'lesson');

  // The next lesson is the likeliest tap: have it ready.
  if (lesson.next) {
    const next = content.lesson(lesson.next.id);
    preloadContent(source, next.key, next.fetch);
  }

  return (
    <div className="lg:grid lg:grid-cols-[minmax(0,1fr)_13rem] lg:gap-14">
      <title>{`${lesson.title} · LLD Guide`}</title>
      <article className="min-w-0">
        <BackLink to={`/learn/${module.slug}`}>{module.title}</BackLink>
        <MatchBar marks={matches} />
        <LessonHeader
          lesson={lesson}
          module={module}
          position={position + 1}
          reading={reading}
          marks={marks}
          onOpenNotes={() => notes.openNotes()}
        />
        <LookalikeContext value={{ module, lessonSlug: lesson.slug }}>
          <SectionAnnotationsContext value={notes.annotations}>
            <div ref={body}>
              <MarkdownView markdown={lesson.bodyMarkdown} terms={lesson.terms} textSize={textSize} />
            </div>
          </SectionAnnotationsContext>
        </LookalikeContext>
        <NotesSheet
          lessonId={lesson.id}
          lessonTitle={lesson.title}
          sections={notes.sections}
          currentAnchor={sectionOf(lesson.outline, reading.current)}
          open={notes.open}
          focus={notes.focus}
          onOpenChange={notes.setOpen}
        />
        <footer className="mt-16 border-t border-rule pt-8">
          <DoneControl lessonId={lesson.id} sectionCount={lesson.outline.length} />
          <NextUp lesson={lesson} />
        </footer>
      </article>
      <aside className="hidden lg:block">
        <OutlineColumn outline={lesson.outline} current={reading.current} read={reading.read} marks={marks} />
      </aside>
    </div>
  );
}

/** The h2 a heading belongs to (the h2 itself, or the one above an h3): notes and bookmarks sit on h2 sections. */
function sectionOf(outline: OutlineEntry[], anchor: string | null): string {
  let section = '';
  for (const entry of outline) {
    if (entry.level === 2) section = entry.anchor;
    if (entry.anchor === anchor) return section;
  }
  return '';
}

/**
 * The lesson's notes sheet (spec 007 §3.2): opened from the header, a heading's chip or actions, or a deep link
 * (`?note=<id>` from the Notes tab). Closing it drops the deep link, so a reload doesn't reopen it.
 */
function useLessonNotesSheet(lesson: LessonDetail) {
  const [params, setParams] = useSearchParams();
  const linkedNote = params.get('note');
  const [state, setState] = useState<{ open: boolean; focus?: NotesFocus }>(() =>
    linkedNote
      ? { open: true, focus: { noteId: linkedNote, anchor: params.get('at') ?? '' } }
      : { open: false },
  );
  const sections = useMemo(
    () =>
      lesson.outline
        .filter((entry) => entry.level === 2)
        .map((entry) => ({ anchor: entry.anchor, title: entry.text })),
    [lesson.outline],
  );
  const annotations = useMemo(
    () => ({
      lessonId: lesson.id,
      sectionTitles: new Map(sections.map((section) => [section.anchor, section.title])),
      openNotes: (focus?: NotesFocus) => setState({ open: true, focus }),
    }),
    [lesson.id, sections],
  );
  const setOpen = (open: boolean) => {
    setState((current) => ({ ...current, open }));
    if (!open && linkedNote) {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          next.delete('note');
          return next;
        },
        { replace: true },
      );
    }
  };
  return { ...state, sections, annotations, openNotes: annotations.openNotes, setOpen };
}

function LessonHeader({
  lesson,
  module,
  position,
  reading,
  marks,
  onOpenNotes,
}: {
  lesson: LessonDetail;
  module: ModuleDetail;
  position: number;
  reading: ReturnType<typeof useReadingTracker>;
  marks: ReturnType<typeof useSectionMarks>;
  onOpenNotes: () => void;
}) {
  const noteCount = useLessonNotes(lesson.id).length;
  return (
    <header className="mt-3 border-b border-rule pb-6">
      <p className="font-mono text-[12px] tracking-[0.08em] text-muted tabular-nums">
        lesson {String(position).padStart(2, '0')} of {String(module.lessons.length).padStart(2, '0')} ·{' '}
        {formatMinutes(lesson.readingMinutes)} read
      </p>
      <div className="mt-1.5 flex items-start gap-3 text-[32px] md:text-[44px]">
        <LessonIcon icon={lesson.icon} className="lesson-icon-tile mt-[0.05em]" />
        <h1 className="min-w-0 text-[1em] font-semibold">{lesson.title}</h1>
      </div>
      <p className="mt-3 max-w-prose text-[17px] leading-relaxed text-muted">{lesson.summary}</p>
      <div className="mt-5 flex flex-wrap items-center gap-2">
        <Link
          to={`/revise/${lesson.moduleSlug}/${lesson.slug}`}
          className={cn(
            'inline-flex h-10 items-center gap-2 rounded-full bg-accent px-4 text-[14px] font-medium text-on-fill select-none',
            'transition-[transform,opacity] duration-[120ms] ease-out active:scale-[0.97] hover:opacity-90',
            'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
          )}
        >
          <Icon d={boltIcon} size={16} />
          Cheat sheet
        </Link>
        <div className="lg:hidden">
          <OutlineSheet
            outline={lesson.outline}
            current={reading.current}
            read={reading.read}
            marks={marks}
          />
        </div>
        <button
          type="button"
          onClick={onOpenNotes}
          className={cn(
            'inline-flex h-10 items-center gap-2 rounded-full border border-rule bg-surface px-3.5 text-[14px] font-medium select-none',
            'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
            'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
          )}
        >
          <Icon d={pencilIcon} size={16} />
          Notes
          {noteCount > 0 && (
            <span className="font-mono text-[13px] text-muted tabular-nums">· {noteCount}</span>
          )}
        </button>
        <BookmarkToggle kind="lesson" lessonId={lesson.id} />
        <TextSizeControl />
      </div>
    </header>
  );
}
