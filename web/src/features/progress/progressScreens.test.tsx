import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { toast } from 'sonner';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import type { CheatSheetSummary, DataSource, LessonDetail, LessonProgress, LessonSummary, ModuleDetail } from '../../data/types';
import { AppProviders } from '../../test/AppProviders';
import { fakeDataSource } from '../../test/fakeDataSource';
import { FLUSH_DELAY_MS } from './progressStore';
import { StatusMark } from './StatusMark';

const lesson = (moduleSlug: string, slug: string, title: string): LessonSummary => ({
  id: slug,
  moduleSlug,
  slug,
  title,
  summary: `About ${title}.`,
  order: 1,
  readingMinutes: 5,
  tags: [],
  sectionCount: 4,
});

const modules: ModuleDetail[] = [
  { slug: 'start', title: 'Start here', summary: 'x', lessons: [lesson('start', 'how-to-use', 'How to use')] },
  {
    slug: 'oop-solid',
    title: 'OOP & SOLID',
    summary: 'x',
    lessons: [lesson('oop-solid', 'srp', 'SRP'), lesson('oop-solid', 'ocp', 'OCP'), lesson('oop-solid', 'lsp', 'LSP')],
  },
];

const record = (lessonId: string, status: LessonProgress['status'], extra: Partial<LessonProgress> = {}): LessonProgress => ({
  lessonId,
  status,
  doneBy: status === 'done' ? 'manual' : null,
  completedAt: status === 'done' ? '2026-10-18T09:00:00Z' : null,
  readSections: ['a', 'b'],
  position: null,
  lastReadAt: '2026-10-18T09:00:00Z',
  lastRevisedAt: null,
  updatedAt: '2026-10-18T09:00:00Z',
  deviceId: 'd',
  ...extra,
});

const sheet = (moduleSlug: string, moduleTitle: string, lessonId: string, title: string): CheatSheetSummary => ({
  lessonId,
  moduleSlug,
  moduleTitle,
  lessonSlug: lessonId,
  lessonTitle: title,
  title: `Cheat sheet: ${title}`,
});

function source(lessons: LessonProgress[] = [], activityDays: string[] = []): DataSource {
  return fakeDataSource({
    getModules: vi.fn().mockResolvedValue(
      modules.map((m, order) => ({ slug: m.slug, title: m.title, summary: m.summary, order, lessonCount: m.lessons.length })),
    ),
    getModule: vi.fn().mockImplementation((slug: string) => Promise.resolve(modules.find((m) => m.slug === slug))),
    getCheatSheets: vi
      .fn()
      .mockResolvedValue([
        sheet('start', 'Start here', 'how-to-use', 'How to use'),
        sheet('oop-solid', 'OOP & SOLID', 'srp', 'SRP'),
        sheet('oop-solid', 'OOP & SOLID', 'ocp', 'OCP'),
      ]),
    getProgress: vi.fn().mockResolvedValue({ lessons, activityDays }),
    getLesson: vi.fn().mockImplementation((id: string) => Promise.resolve(detail(id))),
  });
}

function detail(id: string): LessonDetail {
  const summary = modules.flatMap((m) => m.lessons).find((l) => l.id === id)!;
  return {
    ...summary,
    moduleTitle: modules.find((m) => m.slug === summary.moduleSlug)!.title,
    bodyMarkdown: ['# Title', '', '## One', '', 'Text.'].join(String.fromCharCode(10)),
    outline: [{ level: 2, text: 'One', anchor: 'one' }],
    cheatSheetTitle: 'Cheat sheet',
    previous: null,
    next: null,
    terms: [],
  };
}

async function renderAt(path: string, data: DataSource) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <AppProviders source={data}>
        <RouterProvider router={router} />
      </AppProviders>,
    );
  });
  return router;
}

afterEach(() => {
  window.localStorage.clear();
  vi.useRealTimers();
});

describe('StatusMark', () => {
  it.each([
    ['notStarted', 'Not started'],
    ['inProgress', 'In progress'],
    ['done', 'Done'],
  ] as const)('reads %s as "%s"', (status, label) => {
    render(<StatusMark status={status} />);

    expect(screen.getByRole('img', { name: label })).toBeInTheDocument();
  });
});

describe('Learn with progress', () => {
  it('continues the lesson in progress, with the overall count', async () => {
    await renderAt('/learn', source([record('how-to-use', 'done'), record('ocp', 'inProgress')]));

    const card = await screen.findByRole('link', { name: 'Continue: OCP' });
    expect(card).toHaveAttribute('href', '/learn/oop-solid/ocp');
    expect(card).toHaveTextContent('1 of 4 done');
    expect(card).toHaveTextContent('2 of 4 sections read');
  });

  it('offers the next lesson to start when nothing is in progress', async () => {
    await renderAt('/learn', source([record('how-to-use', 'done')]));
    expect(await screen.findByRole('link', { name: 'Start: SRP' })).toBeInTheDocument();
  });

  it('hides Continue once everything is done', async () => {
    const all = ['how-to-use', 'srp', 'ocp', 'lsp'].map((id) => record(id, 'done'));

    await renderAt('/learn', source(all));

    await screen.findByRole('list', { name: 'Modules in reading order' });
    expect(screen.queryByRole('link', { name: /^(Continue|Start):/ })).not.toBeInTheDocument();
  });

  it('inks a finished module and marks each lesson', async () => {
    await renderAt('/learn', source([record('how-to-use', 'done'), record('srp', 'inProgress')]));

    const start = (await screen.findByRole('heading', { name: 'Start here' })).closest('article')!;
    const oop = screen.getByRole('heading', { name: 'OOP & SOLID' }).closest('article')!;
    expect(start).toHaveAttribute('data-inked');
    expect(oop).not.toHaveAttribute('data-inked');
    expect(within(oop).getByLabelText('0 of 3 lessons done')).toHaveTextContent('0/3');
    expect(within(oop).getAllByRole('img').map((mark) => mark.getAttribute('aria-label'))).toEqual([
      'In progress',
      'Not started',
      'Not started',
    ]);
  });
});

describe('Revise with progress', () => {
  it('says when each sheet was revised and can show only the ones not revised yet', async () => {
    const today = new Date().toISOString();
    await renderAt('/revise', source([record('srp', 'done', { lastRevisedAt: today })]));

    expect(await screen.findByText('Revised today')).toBeInTheDocument();
    expect(screen.getAllByText('Not revised yet')).toHaveLength(2);

    await act(async () => fireEvent.click(screen.getByRole('radio', { name: 'Not revised' })));

    expect(screen.queryByText('Cheat sheet: SRP')).not.toBeInTheDocument();
    expect(screen.getByText('Cheat sheet: OCP')).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: 'Not revised' })).toHaveAttribute('aria-checked', 'true');
  });
});

describe('Profile progress', () => {
  it('shows the overall count, each module and this week', async () => {
    const today = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    const day = `${today.getFullYear()}-${pad(today.getMonth() + 1)}-${pad(today.getDate())}`;

    await renderAt('/profile', source([record('srp', 'done'), record('ocp', 'done')], [day]));

    const progress = await screen.findByRole('region', { name: 'Progress' });
    expect(within(progress).getByText('lessons done').parentElement).toHaveTextContent('2 of 4');
    expect(within(progress).getByLabelText('2 of 3 lessons done')).toBeInTheDocument();
    expect(within(progress).getByText('1 day this week')).toBeInTheDocument();
    expect(within(progress).getByRole('listitem', { name: /studied \(today\)/ })).toBeInTheDocument();
  });

  it('resets one module after the learner picks it, and says so', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const data = source([record('srp', 'done'), record('how-to-use', 'done')]);
    await renderAt('/profile', data);

    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Reset progress' })));
    const confirm = await screen.findByRole('button', { name: 'Choose what to reset' });
    expect(confirm).toBeDisabled();
    await act(async () => fireEvent.click(screen.getByRole('radio', { name: 'OOP & SOLID' })));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Reset OOP & SOLID' })));
    await act(async () => vi.advanceTimersByTimeAsync(FLUSH_DELAY_MS));

    const calls = vi.mocked(data.saveLessonProgress).mock.calls;
    const saved = calls[calls.length - 1][0];
    expect(saved.map((r) => [r.lessonId, r.status])).toEqual([
      ['srp', 'notStarted'],
      ['ocp', 'notStarted'],
      ['lsp', 'notStarted'],
    ]);
    const progress = screen.getByRole('region', { name: 'Progress' });
    expect(within(progress).getByText('lessons done').parentElement).toHaveTextContent('1 of 4');
  });
});

describe('Module complete (spec 005 §3.8)', () => {
  it('presses the «complete» stamp in the first time only', async () => {
    const data = source([record('how-to-use', 'done')]);
    const { unmount } = await renderView('/learn/start', data);

    expect(await screen.findByText('«complete»')).toHaveAttribute('data-fresh');
    expect(screen.getByRole('region', { name: 'Lessons' })).toHaveAttribute('data-inked');
    unmount();

    await renderView('/learn/start', source([record('how-to-use', 'done')]));
    expect(await screen.findByText('«complete»')).not.toHaveAttribute('data-fresh');
  });

  it('shows no stamp while a lesson is left', async () => {
    await renderView('/learn/oop-solid', source([record('srp', 'done')]));

    await screen.findByRole('region', { name: 'Lessons' });
    expect(screen.queryByText('«complete»')).not.toBeInTheDocument();
  });

  it('says so with a toast when the last lesson is finished from the lesson page', async () => {
    const success = vi.spyOn(toast, 'success');
    await renderView('/learn/start/how-to-use', source());
    // The reader is loaded on first use, which can take a moment in a cold test run.
    const mark = await screen.findByRole('button', { name: 'Mark as done' }, { timeout: 5000 });

    await act(async () => fireEvent.click(mark));

    expect(success).toHaveBeenCalledWith('Start here complete', { description: 'All 1 lessons done.' });
    success.mockRestore();
  });
});

async function renderView(path: string, data: DataSource) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  let view!: ReturnType<typeof render>;
  await act(async () => {
    view = render(
      <AppProviders source={data}>
        <RouterProvider router={router} />
      </AppProviders>,
    );
  });
  return view;
}
