import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { handleBack } from '../../../app/hardwareBack';
import { routes } from '../../../app/routes';
import type {
  CheatSheetDetail,
  DataSource,
  LessonDetail,
  ModuleDetail,
  SearchRequest,
} from '../../../data/types';
import { AppProviders } from '../../../test/AppProviders';
import { fakeDataSource, noResults } from '../../../test/fakeDataSource';
import { saveRecall } from '../../content/reader/readerStore';
import { findRanges, firstFrom, wordPattern } from './findRanges';

describe('findRanges', () => {
  it('finds whole words, ignoring case, in document order, skipping code and diagrams', () => {
    document.body.innerHTML =
      '<div id="root"><p>Inheritance: a class <em>inherits</em> members.</p><pre>inherits</pre><svg><text>inherits</text></svg><p>Inheriting is not inherits2.</p></div>';

    const ranges = findRanges(document.getElementById('root')!, ['inherits', 'Inheritance']);

    expect(ranges.map((r) => r.toString())).toEqual(['Inheritance', 'inherits']);
  });

  it('builds no pattern for no words and escapes regex characters', () => {
    expect(wordPattern([])).toBeNull();
    expect(wordPattern(['c++'])!.test('use c++ here')).toBe(true);
  });

  it('starts at the first mark after a section heading, or the first mark at all', () => {
    document.body.innerHTML = '<p>wrap one</p><h2 id="the-idea">The idea</h2><p>wrap two</p>';
    const ranges = findRanges(document.body, ['wrap']);

    expect(firstFrom(ranges, document.getElementById('the-idea'))).toBe(1);
    expect(firstFrom(ranges, null)).toBe(0);
    expect(firstFrom([], null)).toBe(-1);
  });
});

const decorator: LessonDetail = {
  id: 'decorator',
  moduleSlug: 'gof-patterns',
  moduleTitle: 'GoF Design Patterns',
  slug: 'decorator',
  title: 'Decorator',
  summary: 'Wrap it.',
  order: 1,
  readingMinutes: 2,
  tags: [],
  bodyMarkdown:
    '# Decorator\n\nWrap an object.\n\n## The idea\n\nA wrapper wraps another object.\n\n## Go deeper\n\n### Order\n\nThe order of wrappers matters.',
  outline: [
    { level: 2, text: 'The idea', anchor: 'the-idea' },
    { level: 2, text: 'Go deeper', anchor: 'go-deeper' },
  ],
  cheatSheetTitle: 'Decorator',
  previous: null,
  next: null,
  terms: [],
};

const module: ModuleDetail = {
  slug: 'gof-patterns',
  title: 'GoF Design Patterns',
  summary: '',
  lessons: [{ ...decorator, sectionCount: 2 }],
};

const sheet = {
  lessonId: 'decorator',
  lessonSlug: 'decorator',
  lessonTitle: 'Decorator',
  moduleSlug: 'gof-patterns',
  moduleTitle: 'GoF Design Patterns',
  title: 'Cheat sheet: Decorator',
  bodyMarkdown: '## Use it when\n\nYou add behaviour.\n\n## Shape\n\nSame interface, wraps one.',
} as CheatSheetDetail;

function source(words: string[]): DataSource {
  return fakeDataSource({
    getModule: vi.fn().mockResolvedValue(module),
    getLesson: vi.fn().mockResolvedValue(decorator),
    getCheatSheet: vi.fn().mockResolvedValue(sheet),
    search: vi.fn().mockImplementation(async ({ query }: SearchRequest) => ({ ...noResults(query), words })),
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

beforeAll(async () => {
  await import('../../content/LessonPage');
  await import('../../content/CheatSheetPage');
});

afterEach(() => localStorage.clear());

describe('Search marks in the reader (spec 009 §3.6)', () => {
  it('counts the marks, starts in the section the result pointed at, steps and wraps around', async () => {
    const data = source(['wrapper', 'wraps', 'Wrap', 'wrappers']);
    await renderAt('/learn/gof-patterns/decorator?at=the-idea&q=wrap', data);

    const bar = await screen.findByRole('group', { name: 'Search matches' });
    expect(data.search).toHaveBeenCalledWith({ query: 'wrap', lessonId: 'decorator', kind: 'lesson' });
    expect(bar).toHaveTextContent('“wrap”2 of 4');

    fireEvent.click(screen.getByRole('button', { name: 'Next match' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next match' }));
    expect(bar).toHaveTextContent('4 of 4');
    fireEvent.click(screen.getByRole('button', { name: 'Next match' }));
    expect(bar).toHaveTextContent('1 of 4');
  });

  it('opens Go deeper when the current mark is inside it', async () => {
    await renderAt('/learn/gof-patterns/decorator?at=go-deeper&q=order', source(['order']));

    const bar = await screen.findByRole('group', { name: 'Search matches' });
    expect(bar).toHaveTextContent('1 of 2');

    await waitFor(() => expect(screen.getByText(/The order of wrappers matters/)).toBeVisible());
  });

  it('opened at the top, counts the marks without choosing one; the first step goes to the first mark', async () => {
    await renderAt('/learn/gof-patterns/decorator?q=wrap', source(['wrapper', 'wraps', 'Wrap', 'wrappers']));

    const bar = await screen.findByRole('group', { name: 'Search matches' });
    expect(bar).toHaveTextContent('4 matches');
    fireEvent.click(screen.getByRole('button', { name: 'Next match' }));
    expect(bar).toHaveTextContent('1 of 4');
  });

  it('clears the marks and drops q from the URL with the button, Escape or Android Back', async () => {
    const router = await renderAt('/learn/gof-patterns/decorator?at=the-idea&q=wrap', source(['wraps']));
    await screen.findByRole('group', { name: 'Search matches' });

    fireEvent.click(screen.getByRole('button', { name: 'Clear search marks' }));
    expect(router.state.location.search).toBe('?at=the-idea');
    expect(screen.queryByRole('group', { name: 'Search matches' })).not.toBeInTheDocument();

    await act(async () => {
      await router.navigate('/learn/gof-patterns/decorator?q=wrap');
    });
    await screen.findByRole('group', { name: 'Search matches' });
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(router.state.location.search).toBe('');

    await act(async () => {
      await router.navigate('/learn/gof-patterns/decorator?q=wrap');
    });
    await screen.findByRole('group', { name: 'Search matches' });
    let result: string | undefined;
    act(() => {
      result = handleBack();
    });
    expect(result).toBe('closed');
    expect(router.state.location.search).toBe('');
  });

  it('shows no bar when the words are not on the page', async () => {
    await renderAt('/learn/gof-patterns/decorator?q=zebra', source(['zebra']));
    await screen.findByRole('heading', { level: 1, name: 'Decorator' });

    expect(screen.queryByRole('group', { name: 'Search matches' })).not.toBeInTheDocument();
  });

  it('on a cheat sheet in recall mode, reveals only the matched section (spec Q8)', async () => {
    saveRecall(true);
    const data = source(['wraps']);
    await renderAt('/revise/gof-patterns/decorator?at=shape&q=wraps', data);

    await screen.findByRole('group', { name: 'Search matches' });

    expect(data.search).toHaveBeenCalledWith({ query: 'wraps', lessonId: 'decorator', kind: 'cheatSheet' });
    await waitFor(() => expect(screen.getByText(/Same interface, wraps one/)).toBeVisible());
    expect(screen.getByText(/You add behaviour/)).not.toBeVisible();
  });
});
