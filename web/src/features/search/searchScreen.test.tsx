import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import type { DataSource, SearchHit, SearchRequest, SearchResults } from '../../data/types';
import { AppProviders } from '../../test/AppProviders';
import { fakeDataSource, noResults } from '../../test/fakeDataSource';
import { loadRecent } from './recentSearches';
import { SEARCH_DEBOUNCE_MS } from './useSearch';

const hit = (over: Partial<SearchHit>): SearchHit => ({
  kind: 'lesson',
  moduleSlug: 'gof-patterns',
  lessonId: 'decorator',
  lessonSlug: 'decorator',
  lessonTitle: 'Decorator',
  anchor: '',
  heading: '',
  noteId: null,
  snippet: { text: 'Wrap it to add, layer by layer.', marks: [[0, 4]] },
  titleMatch: true,
  ...over,
});

const decorResults: SearchResults = {
  query: 'decor',
  expansions: [],
  total: 3,
  countsByKind: { lesson: 2, cheatSheet: 1, note: 0 },
  hits: [
    hit({}),
    hit({ anchor: 'the-idea', heading: 'The idea', snippet: { text: 'A decorator wraps', marks: [[2, 9]] } }),
    hit({
      kind: 'cheatSheet',
      moduleSlug: 'uml',
      lessonId: 'class',
      lessonSlug: 'class',
      lessonTitle: 'Classes',
      heading: 'Rule',
    }),
  ],
  words: [],
  suggestions: [],
};

function source(search: (request: SearchRequest) => Promise<SearchResults>): DataSource {
  return fakeDataSource({
    getModules: vi.fn().mockResolvedValue([
      { slug: 'gof-patterns', title: 'GoF Design Patterns', summary: '', order: 2, lessonCount: 23 },
      { slug: 'uml', title: 'UML', summary: '', order: 4, lessonCount: 10 },
    ]),
    search: vi.fn().mockImplementation(search),
  });
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

/** Lets the debounce pass and the search answer. */
async function settle() {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, SEARCH_DEBOUNCE_MS + 20));
  });
}

afterEach(() => localStorage.clear());

describe('The Search tab (spec 009 §3.4)', () => {
  it('shows a Best match, then module groups with counts, and marks the matched words', async () => {
    await renderAt(
      '/search?q=decor',
      source(async () => decorResults),
    );
    await settle();

    expect(screen.getByText(/results$/)).toHaveTextContent('3 results');
    const best = screen.getByRole('link', { name: /Best match/ });
    expect(within(best).getByText('Decorator', { selector: '.search-best-title' })).toBeInTheDocument();
    expect(within(best).getByText('Wrap', { selector: 'mark' })).toBeInTheDocument();
    const patterns = screen.getByRole('region', { name: 'GoF Design Patterns' });
    expect(within(patterns).getByRole('link', { name: /Decorator › The idea/ })).toHaveAttribute(
      'href',
      '/learn/gof-patterns/decorator?at=the-idea&q=decor',
    );
    const uml = screen.getByRole('region', { name: 'UML' });
    expect(within(uml).getByRole('link', { name: /cheat sheet/i })).toHaveAttribute(
      'href',
      '/revise/uml/class?q=decor',
    );
  });

  it("filters by kind and module through the URL, and the chips keep every kind's count", async () => {
    const data = source(async ({ query }) => ({ ...decorResults, query }));
    const router = await renderAt('/search?q=decor', data);
    await settle();

    fireEvent.click(screen.getByRole('button', { name: /Cheat sheets/ }));
    await settle();
    fireEvent.change(screen.getByRole('combobox', { name: 'Module' }), { target: { value: 'uml' } });
    await settle();

    expect(router.state.location.search).toBe('?q=decor&kind=cheatSheet&module=uml');
    expect(data.search).toHaveBeenLastCalledWith({
      query: 'decor',
      kind: 'cheatSheet',
      module: 'uml',
      limit: undefined,
    });
    expect(screen.getByRole('button', { name: /Cheat sheets/ })).toHaveAttribute('aria-pressed', 'true');
    expect(within(screen.getByRole('button', { name: /Lessons/ })).getByText('2')).toBeInTheDocument();
  });

  it('says what it also searched and offers to drop the filters when nothing matched', async () => {
    const data = source(async ({ query }) => ({
      ...noResults(query),
      expansions: ['single responsibility'],
    }));
    const router = await renderAt('/search?q=srp&kind=note', data);
    await settle();

    expect(screen.getByText(/No matches for/)).toHaveTextContent('No matches for srp.');
    expect(screen.getByText(/Also searching: single responsibility/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Search everything instead' }));

    expect(router.state.location.search).toBe('?q=srp');
  });

  it('with an empty box shows suggestions, and a tap searches for one', async () => {
    const data = source(async ({ query }) => noResults(query, ['SOLID', 'Observer']));
    const router = await renderAt('/search', data);
    await settle();

    fireEvent.click(screen.getByRole('button', { name: 'Observer' }));

    expect(router.state.location.search).toBe('?q=Observer');
  });

  it('saves a recent search when a result is opened, shows it on an empty box, and clears it', async () => {
    const data = source(async ({ query }) => (query === 'decor' ? decorResults : noResults(query)));
    const router = await renderAt('/search?q=decor', data);
    await settle();

    fireEvent.click(screen.getByRole('link', { name: /Best match/ }));
    expect(loadRecent('guest').map((r) => r.query)).toEqual(['decor']);

    await act(async () => {
      await router.navigate('/search');
    });
    await settle();
    expect(screen.getByRole('button', { name: /decor/ })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));

    expect(screen.queryByRole('heading', { name: 'Recent' })).not.toBeInTheDocument();
    expect(loadRecent('guest')).toEqual([]);
  });

  it('opens the top result on Enter, moves through results with the arrows, clears on Escape', async () => {
    const router = await renderAt(
      '/search?q=decor',
      source(async () => decorResults),
    );
    await settle();
    const box = screen.getByRole('searchbox');

    fireEvent.keyDown(box, { key: 'ArrowDown' });
    expect(screen.getByRole('link', { name: /Best match/ })).toHaveFocus();
    fireEvent.keyDown(document.activeElement!, { key: 'ArrowDown' });
    expect(screen.getByRole('link', { name: /Decorator › The idea/ })).toHaveFocus();
    fireEvent.keyDown(document.activeElement!, { key: 'ArrowUp' });
    fireEvent.keyDown(document.activeElement!, { key: 'ArrowUp' });
    expect(box).toHaveFocus();

    fireEvent.keyDown(box, { key: 'Escape' });
    expect(router.state.location.search).toBe('');

    fireEvent.change(box, { target: { value: 'decor' } });
    await settle();
    fireEvent.keyDown(box, { key: 'Enter' });
    await waitFor(() => expect(router.state.location.pathname).toBe('/learn/gof-patterns/decorator'));
  });

  it('drops an older answer that arrives after a newer one', async () => {
    let answerSlow: (results: SearchResults) => void = () => {};
    const data = source(({ query }) =>
      query === 'obs'
        ? new Promise<SearchResults>((resolve) => {
            answerSlow = resolve;
          })
        : Promise.resolve({ ...decorResults, query }),
    );
    await renderAt('/search?q=obs', data);
    await settle();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'decor' } });
    await settle();

    await act(async () => answerSlow({ ...noResults('obs') }));

    expect(screen.getByRole('link', { name: /Best match/ })).toBeInTheDocument();
  });
});
