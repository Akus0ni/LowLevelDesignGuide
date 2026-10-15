import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import { DataSourceProvider } from '../../data/DataSourceContext';
import type { CheatSheetDetail, CheatSheetSummary, DataSource, ModuleDetail } from '../../data/types';
import { fakeDataSource } from '../../test/fakeDataSource';
import { AuthProvider } from '../auth/AuthProvider';
import { ServerStatusProvider } from '../serverStatus/ServerStatusProvider';

vi.mock('./markdown/mermaid', async (original) => ({
  ...(await original<typeof import('./markdown/mermaid')>()),
  renderMermaid: vi.fn(() => Promise.resolve('<svg><text>diagram</text></svg>')),
}));

const summary = (
  moduleSlug: string,
  moduleTitle: string,
  lessonSlug: string,
  title: string,
): CheatSheetSummary => ({
  lessonId: `${moduleSlug}-${lessonSlug}`,
  moduleSlug,
  moduleTitle,
  lessonSlug,
  lessonTitle: `Lesson ${title}`,
  title: `Cheat sheet: ${title}`,
});

const sheets = [
  summary('oop-solid', 'OOP & SOLID', 'srp', 'SRP'),
  summary('oop-solid', 'OOP & SOLID', 'ocp', 'OCP'),
  summary('case-studies', 'Case studies', 'auth', 'Auth'),
];

const authSheet: CheatSheetDetail = {
  ...sheets[2],
  bodyMarkdown: [
    '# Cheat sheet: Auth',
    '',
    'Intro stays visible.',
    '',
    '## Requirements',
    '',
    '- Rotate refresh tokens.',
    '',
    '## 3 gotchas',
    '',
    '1. Single-flight refresh.',
  ].join('\n'),
};

const caseStudies: ModuleDetail = {
  slug: 'case-studies',
  title: 'Case studies',
  summary: 'x',
  lessons: [
    {
      id: 'case-studies-auth',
      moduleSlug: 'case-studies',
      slug: 'auth',
      title: 'Auth',
      summary: 'x',
      order: 1,
      readingMinutes: 8,
      tags: [],
    },
  ],
};

function source(): DataSource {
  return fakeDataSource({
    getCheatSheets: vi.fn().mockResolvedValue(sheets),
    getModule: vi.fn().mockResolvedValue(caseStudies),
    getCheatSheet: vi.fn().mockResolvedValue(authSheet),
  });
}

async function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <DataSourceProvider source={source()}>
        <ServerStatusProvider>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </ServerStatusProvider>
      </DataSourceProvider>,
    );
  });
  return router;
}

afterEach(() => window.localStorage.clear());

describe('Revise', () => {
  it('groups every cheat sheet by module', async () => {
    await renderAt('/revise');

    const oop = screen.getByRole('region', { name: 'OOP & SOLID' });
    expect(
      within(oop)
        .getAllByRole('link')
        .map((link) => link.getAttribute('href')),
    ).toEqual(['/revise/oop-solid/srp', '/revise/oop-solid/ocp']);
    expect(within(oop).getByText('2 cheat sheets')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Case studies' })).toBeInTheDocument();
  });

  it('filters by module with chips, keeping the filter in the URL', async () => {
    const router = await renderAt('/revise');

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Case studies' })));

    expect(router.state.location.search).toBe('?module=case-studies');
    expect(screen.queryByRole('region', { name: 'OOP & SOLID' })).toBeNull();
    expect(screen.getByRole('button', { name: 'Case studies' })).toHaveAttribute('aria-pressed', 'true');

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'All' })));
    expect(screen.getByRole('region', { name: 'OOP & SOLID' })).toBeInTheDocument();
  });
});

describe('Cheat sheet', () => {
  beforeAll(async () => {
    await import('./CheatSheetPage');
  });

  it('shows the sheet with a way back and a link to its lesson', async () => {
    await renderAt('/revise/case-studies/auth');

    expect(screen.getByRole('heading', { level: 1, name: 'Cheat sheet: Auth' })).toBeInTheDocument();
    const main = within(screen.getByRole('main'));
    expect(main.getByRole('link', { name: 'Revise' })).toHaveAttribute('href', '/revise');
    expect(main.getByRole('link', { name: /Lesson Auth/ })).toHaveAttribute(
      'href',
      '/learn/case-studies/auth',
    );
    expect(screen.getByText('Rotate refresh tokens.')).toBeInTheDocument();
  });

  it('recall mode folds every section; reveal one, then all; the switch is remembered', async () => {
    await renderAt('/revise/case-studies/auth');

    await act(async () => fireEvent.click(screen.getByRole('switch', { name: 'Recall' })));

    expect(screen.getByRole('switch', { name: 'Recall' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByText('Intro stays visible.')).toBeVisible();
    expect(screen.getAllByRole('button', { name: /think first, then reveal/ })).toHaveLength(2);
    expect(window.localStorage.getItem('lld.reader.recall')).toBe('true');

    await act(async () =>
      fireEvent.click(screen.getAllByRole('button', { name: /think first, then reveal/ })[0]),
    );
    expect(screen.getAllByRole('button', { name: /think first, then reveal/ })).toHaveLength(1);

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Reveal all' })));
    expect(screen.queryAllByRole('button', { name: /think first, then reveal/ })).toHaveLength(0);
  });
});
