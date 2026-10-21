import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app/routes';
import type { DataSource, SearchHit, SearchRequest } from '../../data/types';
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
  snippet: { text: 'Wrap it.', marks: [] },
  titleMatch: true,
  ...over,
});

const realMatchMedia = window.matchMedia;

/** A desktop with a mouse, or a phone. */
function pointer(fine: boolean) {
  window.matchMedia = (query: string) => ({
    ...realMatchMedia(query),
    matches: fine && query.includes('pointer: fine'),
  });
}

function source(): DataSource {
  const many = Array.from({ length: 12 }, (_, i) =>
    hit({ lessonId: `l${i}`, lessonSlug: `l${i}`, lessonTitle: `Lesson ${i}` }),
  );
  return fakeDataSource({
    search: vi
      .fn()
      .mockImplementation(async ({ query }: SearchRequest) =>
        query.startsWith('dec')
          ? { ...noResults(query), total: 13, hits: [...many, hit({ kind: 'cheatSheet', heading: 'Shape' })] }
          : noResults(query),
      ),
  });
}

async function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <AppProviders source={source()}>
        <RouterProvider router={router} />
      </AppProviders>,
    );
  });
  return router;
}

async function type(text: string) {
  fireEvent.change(screen.getByRole('combobox', { name: 'Search' }), { target: { value: text } });
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, SEARCH_DEBOUNCE_MS + 20));
  });
}

beforeEach(() => pointer(true));
afterEach(() => {
  window.matchMedia = realMatchMedia;
  localStorage.clear();
});

describe('Quick search (spec 009 §3.7)', () => {
  it('opens with Ctrl+K, shows at most 8 hits of a kind, and Enter opens the active one', async () => {
    const router = await renderAt('/learn');

    fireEvent.keyDown(window, { key: 'k', ctrlKey: true });
    await type('decor');

    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(8 + 1 + 1);
    expect(options[options.length - 1]).toHaveTextContent('See all results');
    const box = screen.getByRole('combobox', { name: 'Search' });
    fireEvent.keyDown(box, { key: 'ArrowDown' });
    expect(options[1]).toHaveAttribute('aria-selected', 'true');
    fireEvent.keyDown(box, { key: 'Enter' });

    await waitFor(() => expect(router.state.location.pathname).toBe('/learn/gof-patterns/l1'), {
      timeout: 5000,
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(loadRecent('guest').map((r) => r.query)).toEqual(['decor']);
  });

  it('opens with / outside a text field, and "See all results" opens the Search tab', async () => {
    const router = await renderAt('/learn');

    fireEvent.keyDown(document.body, { key: '/' });
    await type('decor');
    fireEvent.click(screen.getByRole('option', { name: 'See all results' }));

    expect(router.state.location.pathname).toBe('/search');
    expect(router.state.location.search).toBe('?q=decor');
  });

  it('does not open from / while typing in a field', async () => {
    await renderAt('/search');

    fireEvent.keyDown(screen.getByRole('searchbox'), { key: '/' });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('closes on Escape and gives focus back', async () => {
    await renderAt('/learn');
    const link = screen.getAllByRole('link', { name: 'Revise' })[0];
    link.focus();

    fireEvent.keyDown(link, { key: 'k', metaKey: true });
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole('combobox', { name: 'Search' }), { key: 'Escape' });

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(link).toHaveFocus();
  });

  it('is not there on a phone', async () => {
    pointer(false);
    await renderAt('/learn');

    fireEvent.keyDown(window, { key: 'k', ctrlKey: true });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
