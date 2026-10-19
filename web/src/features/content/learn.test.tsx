import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import { AppError, type DataSource, type LessonSummary, type ModuleDetail } from '../../data/types';
import { fakeDataSource } from '../../test/fakeDataSource';
import { AppProviders } from '../../test/AppProviders';

const lesson = (moduleSlug: string, slug: string, title: string, minutes: number): LessonSummary => ({
  id: `${moduleSlug}-${slug}`,
  moduleSlug,
  slug,
  title,
  summary: `About ${title}.`,
  order: 1,
  readingMinutes: minutes,
  sectionCount: 4,
  tags: [],
});

const modules: ModuleDetail[] = [
  {
    slug: 'start',
    title: 'Start here',
    summary: 'How the guide works.',
    lessons: [lesson('start', 'how-to-use', 'How to use', 4)],
  },
  {
    slug: 'oop-solid',
    title: 'OOP & SOLID',
    summary: 'The five principles.',
    lessons: ['srp', 'ocp', 'lsp', 'isp', 'dip'].map((slug, i) =>
      lesson('oop-solid', slug, slug.toUpperCase(), i + 5),
    ),
  },
  { slug: 'patterns', title: 'Design patterns', summary: 'GoF.', lessons: [] },
];

function contentSource(overrides: Partial<DataSource> = {}): DataSource {
  return fakeDataSource({
    getModules: vi
      .fn()
      .mockResolvedValue(
        modules.map(({ lessons, ...m }, order) => ({ ...m, order, lessonCount: lessons.length })),
      ),
    getModule: vi.fn().mockImplementation((slug: string) => {
      const found = modules.find((m) => m.slug === slug);
      return found
        ? Promise.resolve(found)
        : Promise.reject(new AppError('content.not_found', 'Not found.', 404));
    }),
    ...overrides,
  });
}

/** React 19: screens that suspend must first render inside an async act(). */
async function renderAt(path: string, source: DataSource = contentSource()) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <AppProviders source={source}>
        <RouterProvider router={router} />
      </AppProviders>,
    );
  });
  return router;
}

describe('Learn', () => {
  it('draws every module as a class box in reading order, with its lessons as members', async () => {
    await renderAt('/learn');

    const diagram = screen.getByRole('list', { name: 'Modules in reading order' });
    const boxes = within(diagram).getAllByRole('article');
    expect(boxes.map((box) => within(box).getByRole('heading').textContent)).toEqual([
      'Start here',
      'OOP & SOLID',
      'Design patterns',
    ]);
    expect(within(boxes[0]).getByRole('link', { name: /Start here/ })).toHaveAttribute(
      'href',
      '/learn/start',
    );
    expect(within(boxes[0]).getByRole('link', { name: /How to use/ })).toHaveAttribute(
      'href',
      '/learn/start/how-to-use',
    );
  });

  it('lists four lessons per box, then "+ N more" leading to the module', async () => {
    await renderAt('/learn');

    const oop = screen.getByRole('article', { name: 'OOP & SOLID' });
    expect(
      within(oop)
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual([
      expect.stringContaining('OOP & SOLID'),
      '+SRP5 min',
      '+OCP6 min',
      '+LSP7 min',
      '+ISP8 min',
      '+ 1 more',
    ]);
    expect(within(oop).getByRole('link', { name: '+ 1 more' })).toHaveAttribute('href', '/learn/oop-solid');
    expect(
      within(screen.getByRole('article', { name: 'Design patterns' })).getByText('no lessons yet'),
    ).toBeInTheDocument();
  });

  it('shows a way to try again when the modules fail to load', async () => {
    const getModules = vi
      .fn()
      .mockRejectedValueOnce(
        new AppError('content.unavailable', 'Lessons could not be loaded on this device.'),
      )
      .mockResolvedValueOnce([]);
    vi.spyOn(console, 'error').mockImplementation(() => {});
    await renderAt('/learn', contentSource({ getModules }));

    expect(screen.getByRole('heading', { name: "Couldn't load this page" })).toBeInTheDocument();
    expect(screen.getByText('Lessons could not be loaded on this device.')).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Try again' })));

    expect(screen.getByRole('list', { name: 'Modules in reading order' })).toBeInTheDocument();
    expect(getModules).toHaveBeenCalledTimes(2);
    vi.mocked(console.error).mockRestore();
  });
});

describe('Module page', () => {
  it('lists the lessons in reading order with their reading time, and links back to Learn', async () => {
    await renderAt('/learn/oop-solid');

    expect(screen.getByRole('heading', { level: 1, name: 'OOP & SOLID' })).toBeInTheDocument();
    expect(screen.getByText('5 lessons · 35 min')).toBeInTheDocument();
    const rows = within(screen.getByRole('region', { name: 'Lessons' })).getAllByRole('link');
    expect(rows.map((row) => row.getAttribute('href'))).toEqual([
      '/learn/oop-solid/srp',
      '/learn/oop-solid/ocp',
      '/learn/oop-solid/lsp',
      '/learn/oop-solid/isp',
      '/learn/oop-solid/dip',
    ]);
    expect(rows[0]).toHaveTextContent('01SRPAbout SRP.5 min');
    expect(within(screen.getByRole('main')).getByRole('link', { name: 'Learn' })).toHaveAttribute(
      'href',
      '/learn',
    );
  });

  it('says when a module has no lessons yet', async () => {
    await renderAt('/learn/patterns');

    expect(screen.getByText('Lessons for this module are being written.')).toBeInTheDocument();
  });

  it('offers a way back for an unknown module', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    await renderAt('/learn/nope');

    expect(screen.getByRole('heading', { name: "This page isn't in the guide" })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Back to Learn' })).toHaveAttribute('href', '/learn');
    vi.mocked(console.error).mockRestore();
  });
});
