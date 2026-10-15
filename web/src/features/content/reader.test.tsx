import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeAll, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import { DataSourceProvider } from '../../data/DataSourceContext';
import { AppError, type DataSource, type LessonDetail, type ModuleDetail } from '../../data/types';
import { fakeDataSource } from '../../test/fakeDataSource';
import { AuthProvider } from '../auth/AuthProvider';
import { ServerStatusProvider } from '../serverStatus/ServerStatusProvider';
import { atNaturalSize } from './markdown/mermaid';

// Mermaid needs a real layout engine; the reader's handling of its output is what's tested here.
vi.mock('./markdown/mermaid', async (original) => ({
  ...(await original<typeof import('./markdown/mermaid')>()),
  renderMermaid: vi.fn((_id: string, source: string) =>
    source.includes('broken')
      ? Promise.reject(new Error('Parse error on line 2'))
      : Promise.resolve('<svg width="100%" style="max-width: 640px;"><text>diagram</text></svg>'),
  ),
}));

const body = [
  '# Case Study 1: Authentication',
  '',
  '## Problem statement',
  '',
  'See [the guide](/learn/start/how-to-use), [rotation](?at=token-rotation) and',
  '[AccessRequest.cs](https://github.com/o/r/blob/main/src/AccessRequest.cs).',
  '',
  '## The `Login` *use* case',
  '',
  '| Entity | Responsibility |',
  '|---|---|',
  '| `RefreshToken` | Rotates on every use |',
  '',
  '```csharp',
  'public Result Approve(DateTimeOffset now) => State.Approve(this, now);',
  '```',
  '',
  '```mermaid',
  'classDiagram',
  '  A --> B',
  '```',
  '',
  '```mermaid',
  'stateDiagram-v2',
  '  broken -->',
  '```',
  '',
  '<script>alert("never rendered")</script>',
  '',
  '### Token rotation',
  '',
  'Done.',
].join('\n');

const auth: LessonDetail = {
  id: 'case-study-auth',
  moduleSlug: 'case-studies',
  moduleTitle: 'Case studies',
  slug: 'auth',
  title: 'Case Study 1: Authentication',
  summary: 'Sign-in, class by class.',
  order: 1,
  readingMinutes: 8,
  tags: [],
  bodyMarkdown: body,
  outline: [
    { level: 2, text: 'Problem statement', anchor: 'problem-statement' },
    { level: 2, text: 'The Login use case', anchor: 'the-login-use-case' },
    { level: 3, text: 'Token rotation', anchor: 'token-rotation' },
  ],
  cheatSheetTitle: 'Cheat sheet: Authentication',
  previous: null,
  next: { id: 'case-study-parking', moduleSlug: 'case-studies', slug: 'parking-lot', title: 'Parking lot' },
};

const caseStudies: ModuleDetail = {
  slug: 'case-studies',
  title: 'Case studies',
  summary: 'Real systems.',
  lessons: [
    { ...auth, tags: [] },
    { ...auth, id: 'case-study-parking', slug: 'parking-lot', title: 'Parking lot', order: 2 },
  ],
};

function readerSource(overrides: Partial<DataSource> = {}): DataSource {
  return fakeDataSource({
    getModule: vi
      .fn()
      .mockImplementation((slug: string) =>
        slug === 'case-studies'
          ? Promise.resolve(caseStudies)
          : Promise.reject(new AppError('content.not_found', 'x', 404)),
      ),
    getLesson: vi
      .fn()
      .mockImplementation((id: string) =>
        id === 'case-study-auth'
          ? Promise.resolve(auth)
          : id === 'case-study-parking'
            ? Promise.resolve({
                ...auth,
                id,
                slug: 'parking-lot',
                title: 'Parking lot',
                previous: null,
                next: null,
              })
            : Promise.reject(new AppError('content.not_found', 'Not found.', 404)),
      ),
    ...overrides,
  });
}

async function renderAt(path: string, source: DataSource = readerSource()) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <DataSourceProvider source={source}>
        <ServerStatusProvider>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </ServerStatusProvider>
      </DataSourceProvider>,
    );
  });
  return { router, source };
}

describe('Lesson reader', () => {
  // The reader route is lazy; load its module once so the router resolves it within act().
  beforeAll(async () => {
    await import('./LessonPage');
  });

  it('shows the title once, its place in the module, and the way back', async () => {
    await renderAt('/learn/case-studies/auth');

    expect(screen.getAllByRole('heading', { level: 1 }).map((h) => h.textContent)).toEqual([
      'Case Study 1: Authentication',
    ]);
    expect(screen.getByText(/lesson 01 of 02 · 8 min read/)).toBeInTheDocument();
    const main = within(screen.getByRole('main'));
    expect(main.getByRole('link', { name: 'Case studies' })).toHaveAttribute('href', '/learn/case-studies');
    expect(main.getByRole('link', { name: 'Cheat sheet' })).toHaveAttribute(
      'href',
      '/revise/case-studies/auth',
    );
  });

  it('gives headings the same ids as the packed outline (GitHub rules, the dropped title still counted)', async () => {
    await renderAt('/learn/case-studies/auth');

    for (const entry of auth.outline) {
      expect(document.getElementById(entry.anchor)).toHaveTextContent(entry.text);
    }
  });

  it('routes app links in the app, keeps anchors on the page and opens web links outside', async () => {
    const { router, source } = await renderAt('/learn/case-studies/auth');

    expect(screen.getByRole('link', { name: 'the guide' })).toHaveAttribute(
      'href',
      '/learn/start/how-to-use',
    );
    await act(async () => fireEvent.click(screen.getByRole('link', { name: 'rotation' })));
    expect(router.state.location.search).toBe('?at=token-rotation');

    await act(async () => fireEvent.click(screen.getByRole('link', { name: /AccessRequest\.cs/ })));
    expect(source.openExternal).toHaveBeenCalledWith('https://github.com/o/r/blob/main/src/AccessRequest.cs');
  });

  it('wraps tables and code so only they scroll sideways, and never renders raw HTML', async () => {
    await renderAt('/learn/case-studies/auth');

    expect(screen.getByRole('region', { name: 'Table' })).toContainElement(screen.getByRole('table'));
    expect(screen.getByLabelText('csharp code')).toHaveTextContent(
      'public Result Approve(DateTimeOffset now)',
    );
    expect(document.querySelector('script')).toBeNull();
    expect(screen.queryByText(/never rendered/)).toBeNull();
  });

  it('draws diagrams, and shows the source of one that fails', async () => {
    await renderAt('/learn/case-studies/auth');

    expect(await screen.findByRole('figure', { name: 'class diagram' })).toHaveTextContent('diagram');
    expect(screen.getByText("This diagram couldn't be drawn, so here is its source.")).toBeInTheDocument();
    expect(screen.getByLabelText('mermaid code')).toHaveTextContent('broken -->');
  });

  it('links to the next lesson in the module', async () => {
    await renderAt('/learn/case-studies/auth');

    const more = within(screen.getByRole('navigation', { name: 'More in this module' }));
    expect(more.getByRole('link', { name: /next\s*Parking lot/ })).toHaveAttribute(
      'href',
      '/learn/case-studies/parking-lot',
    );
  });

  it('says a lesson is not in the guide when the slug is unknown', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    await renderAt('/learn/case-studies/nope');

    expect(screen.getByRole('heading', { name: "This page isn't in the guide" })).toBeInTheDocument();
    vi.mocked(console.error).mockRestore();
  });
});

describe('Diagram viewer sizing', () => {
  it('turns a fitted Mermaid SVG into its natural size', () => {
    expect(atNaturalSize('<svg width="100%" style="max-width: 812.5px;" viewBox="0 0 812.5 400">')).toBe(
      '<svg width="812.5" style="max-width: none;" viewBox="0 0 812.5 400">',
    );
    expect(atNaturalSize('<svg width="300">')).toBe('<svg width="300">');
  });
});
