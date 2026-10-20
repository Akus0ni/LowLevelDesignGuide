import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { toast, Toaster } from 'sonner';
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import {
  AppError,
  type Bookmark,
  type CheatSheetDetail,
  type DataSource,
  type LessonDetail,
  type ModuleDetail,
  type Note,
} from '../../data/types';
import { AppProviders } from '../../test/AppProviders';
import { fakeDataSource } from '../../test/fakeDataSource';
import { AUTOSAVE_MS } from './NoteEditor';

const srp: LessonDetail = {
  id: 'srp',
  moduleSlug: 'oop-solid',
  moduleTitle: 'OOP & SOLID',
  slug: 'srp',
  title: 'Single Responsibility',
  summary: 'One reason to change.',
  order: 1,
  readingMinutes: 2,
  tags: [],
  bodyMarkdown: '# Single Responsibility\n\n## The rule\n\nOne boss.\n\n## See it\n\nSplit it.\n\n### Details\n\nMore.',
  outline: [
    { level: 2, text: 'The rule', anchor: 'the-rule' },
    { level: 2, text: 'See it', anchor: 'see-it' },
    { level: 3, text: 'Details', anchor: 'details' },
  ],
  cheatSheetTitle: 'SRP',
  previous: null,
  next: null,
  terms: [],
};

const module: ModuleDetail = {
  slug: 'oop-solid',
  title: 'OOP & SOLID',
  summary: 'Objects.',
  lessons: [{ ...srp, sectionCount: 3 }],
};

const sheet: CheatSheetDetail = {
  lessonId: 'srp',
  lessonSlug: 'srp',
  lessonTitle: 'Single Responsibility',
  moduleSlug: 'oop-solid',
  moduleTitle: 'OOP & SOLID',
  title: 'SRP',
  bodyMarkdown: '## Rule\n\nOne boss.',
} as CheatSheetDetail;

const at = (minutes: number) => new Date(Date.UTC(2026, 9, 20, 9, minutes)).toISOString();
const note = (id: string, body: string, anchor = '', minutes = 0): Note => ({
  id,
  lessonId: 'srp',
  anchor,
  body,
  createdAt: at(minutes),
  updatedAt: at(minutes),
  deletedAt: null,
  deviceId: 'd',
});

function source(notes: Note[] = [], bookmarks: Bookmark[] = [], overrides: Partial<DataSource> = {}): DataSource {
  return fakeDataSource({
    getModules: vi.fn().mockResolvedValue([{ slug: 'oop-solid', title: 'OOP & SOLID', summary: '', order: 1, lessonCount: 1 }]),
    getModule: vi.fn().mockImplementation((slug: string) =>
      slug === 'oop-solid' ? Promise.resolve(module) : Promise.reject(new AppError('content.not_found', 'x', 404)),
    ),
    getLesson: vi.fn().mockResolvedValue(srp),
    getCheatSheet: vi.fn().mockResolvedValue(sheet),
    getNotes: vi.fn().mockResolvedValue({ notes, bookmarks }),
    ...overrides,
  });
}

async function renderAt(path: string, data: DataSource = source()) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <AppProviders source={data}>
        <RouterProvider router={router} />
        <Toaster />
      </AppProviders>,
    );
  });
  return { router, data };
}

beforeAll(async () => {
  await import('../content/LessonPage');
  await import('../content/CheatSheetPage');
});

afterEach(() => {
  // Sonner keeps toasts in module state: an Undo from one test must not answer the next.
  toast.dismiss();
  vi.useRealTimers();
  window.localStorage.clear();
  window.sessionStorage.clear();
});

describe('Notes in a lesson (spec 007 §3.2)', () => {
  it('marks sections with notes and bookmarks, beside the heading and in the outline', async () => {
    const bookmark: Bookmark = {
      kind: 'section', lessonId: 'srp', anchor: 'see-it', createdAt: at(0), updatedAt: at(0), deletedAt: null, deviceId: 'd',
    };
    await renderAt('/learn/oop-solid/srp', source([note('a', 'One', 'the-rule'), note('b', 'Two', 'the-rule')], [bookmark]));

    const rule = await screen.findByRole('heading', { level: 2, name: /The rule/ });
    expect(within(rule).getByRole('button', { name: '2 notes on this section' })).toBeInTheDocument();
    expect(within(screen.getByRole('heading', { level: 2, name: /See it/ })).getByRole('img', { name: 'Bookmarked' })).toBeInTheDocument();
    const outline = screen.getAllByRole('navigation', { name: 'On this page' })[0];
    expect(within(outline).getByRole('img', { name: '2 notes' })).toBeInTheDocument();
    expect(within(outline).getByRole('img', { name: 'Bookmarked' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Notes · 2|Notes/ })).toHaveTextContent('Notes· 2');
  });

  it('writes a section note from the heading’s actions, saving as the learner types', async () => {
    const { data } = await renderAt('/learn/oop-solid/srp');
    vi.useFakeTimers({ shouldAdvanceTime: true });

    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Note or bookmark: See it' })));
    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Add note here' })));
    const field = await screen.findByRole('textbox', { name: 'Note on See it' });
    await act(async () => fireEvent.change(field, { target: { value: 'Split by boss #weak' } }));
    await act(async () => vi.advanceTimersByTimeAsync(AUTOSAVE_MS + 1100));

    expect(screen.getByText('Saved')).toBeInTheDocument();
    const calls = vi.mocked(data.saveNotes).mock.calls;
    const saved = calls[calls.length - 1][0][0];
    expect(saved).toMatchObject({ lessonId: 'srp', anchor: 'see-it', body: 'Split by boss #weak', deletedAt: null });
  });

  it('starts from a starter, previews it, and deletes with Undo', async () => {
    await renderAt('/learn/oop-solid/srp', source([note('a', 'Old words', 'the-rule')]));

    await act(async () => fireEvent.click(await screen.findByRole('button', { name: '1 note on this section' })));
    await act(async () => fireEvent.click(await screen.findByRole('button', { name: /Old words/ })));
    expect(screen.queryByRole('group', { name: 'Start from' })).not.toBeInTheDocument();
    await act(async () => fireEvent.change(screen.getByRole('textbox'), { target: { value: '' } }));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Gotcha I hit' })));
    expect((screen.getByRole('textbox') as HTMLTextAreaElement).value).toContain('- **Symptom:**');

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Preview' })));
    expect(screen.getByText('Symptom:')).toBeInTheDocument();

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Delete' })));
    expect(screen.getByText('No notes on this lesson yet. Your words here come back under its cheat sheet in Revise.')).toBeInTheDocument();
    expect(await screen.findByText('Note deleted')).toBeInTheDocument();
  });

  it('deletes a note that was emptied and closed, and never saves an empty new one', async () => {
    const { data } = await renderAt('/learn/oop-solid/srp', source([note('a', 'Gone soon', 'the-rule')]));

    await act(async () => fireEvent.click(await screen.findByRole('button', { name: '1 note on this section' })));
    await act(async () => fireEvent.click(await screen.findByRole('button', { name: /Gone soon/ })));
    await act(async () => fireEvent.change(screen.getByRole('textbox'), { target: { value: '   ' } }));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'All notes' })));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'New note' })));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'All notes' })));

    expect(screen.getByText(/No notes on this lesson yet/)).toBeInTheDocument();
    await act(async () => window.dispatchEvent(new Event('pagehide')));
    const written = vi.mocked(data.saveNotes).mock.calls.flatMap((call) => call[0]);
    expect(written).toHaveLength(1);
    expect(written[0]).toMatchObject({ id: 'a', deletedAt: expect.any(String) });
  });

  it('opens straight at a note from a deep link', async () => {
    await renderAt('/learn/oop-solid/srp?at=the-rule&note=a', source([note('a', 'Deep linked', 'the-rule')]));

    expect(await screen.findByRole('textbox', { name: 'Note on The rule' })).toHaveValue('Deep linked');
  });

  it('bookmarks the lesson with Undo', async () => {
    const { data } = await renderAt('/learn/oop-solid/srp');
    const toggle = await screen.findByRole('button', { name: 'Bookmark lesson' });

    await act(async () => fireEvent.click(toggle));

    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Undo' })));
    expect(toggle).toHaveAttribute('aria-pressed', 'false');
    await act(async () => window.dispatchEvent(new Event('pagehide')));
    const calls = vi.mocked(data.saveBookmarks).mock.calls;
    expect(calls[calls.length - 1][0][0]).toMatchObject({ kind: 'lesson', deletedAt: expect.any(String) });
  });
});

describe('Notes in Revise (spec 007 §3.6)', () => {
  it('folds the lesson’s notes under the cheat sheet, in reading order, with a bookmark toggle', async () => {
    await renderAt('/revise/oop-solid/srp', source([note('b', 'On see it', 'see-it', 0), note('a', 'On the rule', 'the-rule', 5)]));

    expect(await screen.findByRole('button', { name: 'Bookmark cheat sheet' })).toBeInTheDocument();
    const fold = screen.getByRole('button', { name: /Your notes \(2\)/ });
    expect(fold).toHaveAttribute('aria-expanded', 'false');
    await act(async () => fireEvent.click(fold));
    const texts = screen.getAllByText(/^On (see it|the rule)$/).map((element) => element.textContent);
    expect(texts).toEqual(['On the rule', 'On see it']);
    expect(screen.getAllByRole('link', { name: 'Edit in the lesson' })[0]).toHaveAttribute(
      'href',
      '/learn/oop-solid/srp?at=the-rule&note=a',
    );
  });
});

describe('The Notes tab (spec 007 §3.4)', () => {
  it('groups notes by module and lesson, filters by tag and text, and shows notes on removed lessons', async () => {
    const removed = { ...note('r', 'Kept after the lesson left'), lessonId: 'gone' };
    await renderAt('/notes', source([note('a', 'Boss #weak', 'the-rule'), note('b', 'Split #weak #work', 'see-it'), removed]));

    expect(await screen.findByRole('region', { name: 'OOP & SOLID' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Removed lessons' })).toHaveTextContent('Kept after the lesson left');
    expect(screen.getByRole('link', { name: /Boss/ })).toHaveAttribute('href', '/learn/oop-solid/srp?at=the-rule&note=a');

    await act(async () => fireEvent.click(screen.getByRole('button', { name: /#work/ })));
    expect(screen.queryByRole('link', { name: /Boss/ })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Split/ })).toBeInTheDocument();

    await act(async () => fireEvent.change(screen.getByRole('searchbox', { name: 'Filter notes' }), { target: { value: 'nothing' } }));
    expect(screen.getByText('No notes match. Clear a tag or the filter to see more.')).toBeInTheDocument();
  });

  it('exports every note as one Markdown file', async () => {
    const { data } = await renderAt('/notes', source([note('a', 'Boss', 'the-rule')]));

    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Export notes' })));

    const [fileName, markdown] = vi.mocked(data.exportNotes).mock.calls[0];
    expect(fileName).toMatch(/^lld-notes-\d{4}-\d{2}-\d{2}\.md$/);
    expect(markdown).toContain('## OOP & SOLID\n\n### Single Responsibility\n\n#### The rule\n\nBoss');
  });

  it('lists bookmarks and opens each in the right place', async () => {
    const mark = (kind: Bookmark['kind'], anchor = ''): Bookmark => ({
      kind, lessonId: 'srp', anchor, createdAt: at(0), updatedAt: at(0), deletedAt: null, deviceId: 'd',
    });
    await renderAt('/notes', source([], [mark('cheatSheet'), mark('section', 'see-it'), mark('lesson')]));

    await act(async () => fireEvent.click(await screen.findByRole('tab', { name: 'Bookmarks' })));

    const links = (await screen.findAllByRole('link')).filter((link) => link.closest('.bookmark-row'));
    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/learn/oop-solid/srp',
      '/learn/oop-solid/srp?at=see-it',
      '/revise/oop-solid/srp',
    ]);
  });

  it('says what to do when there is nothing yet', async () => {
    await renderAt('/notes');

    expect(await screen.findByText(/No notes yet\. Open a lesson and use/)).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('tab', { name: 'Bookmarks' })));
    expect(await screen.findByText(/Nothing bookmarked yet/)).toBeInTheDocument();
  });
});
