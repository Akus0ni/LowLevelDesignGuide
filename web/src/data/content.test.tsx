import { act, fireEvent, render, screen } from '@testing-library/react';
import { Component, Suspense, type ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fakeDataSource } from '../test/fakeDataSource';
import { BridgeDataSource } from './bridgeDataSource';
import { isWebLink, toCamelCaseDeep } from './camelCase';
import { DataSourceProvider } from './DataSourceContext';
import { HttpDataSource } from './httpDataSource';
import { AppError, type DataSource, type ModuleSummary } from './types';
import { forgetContent, preloadContent, content, useModules } from './useContent';

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });

describe('HttpDataSource content', () => {
  it('reads every content endpoint, escaping path segments', async () => {
    const fetchFn = vi.fn().mockImplementation(() => Promise.resolve(json([])));
    const source = new HttpDataSource('', fetchFn);

    await source.getModules();
    await source.getModule('oop solid');
    await source.getLesson('case-study-auth');
    await source.getCheatSheets();
    await source.getCheatSheets('case-studies');
    await source.getCheatSheet('case-study-auth');

    expect(fetchFn.mock.calls.map(([url]) => url)).toEqual([
      '/api/content/modules',
      '/api/content/modules/oop%20solid',
      '/api/content/lessons/case-study-auth',
      '/api/content/cheatsheets',
      '/api/content/cheatsheets?module=case-studies',
      '/api/content/cheatsheets/case-study-auth',
    ]);
  });

  it('turns a 404 problem into the content.not_found code', async () => {
    const fetchFn = vi.fn().mockResolvedValue(json({ title: 'Not found.', code: 'content.not_found' }, 404));

    await expect(new HttpDataSource('', fetchFn).getLesson('nope')).rejects.toMatchObject({
      code: 'content.not_found',
      status: 404,
    });
  });

  it('opens web links in a new tab and refuses other schemes', async () => {
    const open = vi.spyOn(window, 'open').mockReturnValue(null);
    const source = new HttpDataSource('', vi.fn());

    await source.openExternal('https://github.com/x');
    await expect(source.openExternal('javascript:alert(1)')).rejects.toMatchObject({
      code: 'link.not_allowed',
    });

    expect(open).toHaveBeenCalledOnce();
    expect(open).toHaveBeenCalledWith('https://github.com/x', '_blank', 'noopener,noreferrer');
    open.mockRestore();
  });
});

describe('BridgeDataSource content', () => {
  it('converts nested PascalCase results from C# to the API shape', async () => {
    const bridge = {
      InvokeDotNet: vi.fn().mockResolvedValue({
        Ok: true,
        Value: {
          Id: 'case-study-auth',
          ModuleSlug: 'case-studies',
          Tags: ['state'],
          Outline: [{ Level: 2, Text: 'Problem', Anchor: 'problem' }],
          Previous: null,
          Next: { Id: 'b', ModuleSlug: 'case-studies', Slug: 'b', Title: 'B' },
        },
        Error: null,
      }),
    };

    const lesson = await new BridgeDataSource(bridge).getLesson('case-study-auth');

    expect(lesson).toEqual({
      id: 'case-study-auth',
      moduleSlug: 'case-studies',
      tags: ['state'],
      outline: [{ level: 2, text: 'Problem', anchor: 'problem' }],
      previous: null,
      next: { id: 'b', moduleSlug: 'case-studies', slug: 'b', title: 'B' },
    });
    expect(bridge.InvokeDotNet).toHaveBeenCalledWith('GetLesson', ['case-study-auth']);
  });

  it('passes null for "every module" and surfaces bridge errors as AppErrors', async () => {
    const bridge = {
      InvokeDotNet: vi
        .fn()
        .mockResolvedValueOnce({ Ok: true, Value: [], Error: null })
        .mockResolvedValueOnce({
          Ok: false,
          Value: null,
          Error: { Code: 'content.not_found', Message: "This lesson isn't in this version of the app." },
        }),
    };
    const source = new BridgeDataSource(bridge);

    await expect(source.getCheatSheets()).resolves.toEqual([]);
    await expect(source.getCheatSheet('nope')).rejects.toMatchObject({ code: 'content.not_found' });
    expect(bridge.InvokeDotNet).toHaveBeenNthCalledWith(1, 'GetCheatSheets', [null]);
  });
});

describe('camelCase helpers', () => {
  it('lower-cases the first letter of every key, deeply, leaving values alone', () => {
    expect(toCamelCaseDeep({ A: [{ BC: 'Keep' }], d: null })).toEqual({ a: [{ bC: 'Keep' }], d: null });
  });

  it.each([
    ['https://github.com', true],
    ['http://example.com', true],
    ['javascript:alert(1)', false],
    ['file:///etc/passwd', false],
    ['/learn/start', false],
  ])('isWebLink(%s) is %s', (url, expected) => {
    expect(isWebLink(url)).toBe(expected);
  });
});

const modules: ModuleSummary[] = [
  { slug: 'start', title: 'Start here', summary: 'S', order: 0, lessonCount: 1 },
];

class TestBoundary extends Component<
  { children: ReactNode; onRetry: () => void },
  { error: AppError | null }
> {
  state = { error: null as AppError | null };

  static getDerivedStateFromError(error: AppError) {
    return { error };
  }

  render() {
    if (!this.state.error) return this.props.children;
    return (
      <button
        onClick={() => {
          this.props.onRetry();
          this.setState({ error: null });
        }}
      >
        Try again ({this.state.error.code})
      </button>
    );
  }
}

function ModuleTitles() {
  return (
    <p>
      {useModules()
        .map((m) => m.title)
        .join(', ')}
    </p>
  );
}

/** React 19: a first render that suspends must happen inside an async act(), or React never retries it. */
async function renderModules(source: DataSource) {
  const tree = (
    <DataSourceProvider source={source}>
      <TestBoundary onRetry={() => forgetContent(source, content.modules.key)}>
        <Suspense fallback={<p>Loading…</p>}>
          <ModuleTitles />
        </Suspense>
      </TestBoundary>
    </DataSourceProvider>
  );
  let result: ReturnType<typeof render> | undefined;
  await act(async () => {
    result = render(tree);
  });
  return result!;
}

describe('useContent', () => {
  it('suspends while loading, then renders, and asks the data source only once', async () => {
    let resolve: (value: ModuleSummary[]) => void = () => {};
    const pending = new Promise<ModuleSummary[]>((done) => (resolve = done));
    const source = fakeDataSource({ getModules: vi.fn().mockReturnValue(pending) });

    const first = await renderModules(source);
    expect(screen.getByText('Loading…')).toBeInTheDocument();
    await act(async () => resolve(modules));
    expect(await screen.findByText('Start here')).toBeInTheDocument();
    first.unmount();
    await renderModules(source);

    expect(await screen.findByText('Start here')).toBeInTheDocument();
    expect(source.getModules).toHaveBeenCalledOnce();
  });

  it('shows a failure to the error boundary, and "Try again" asks again', async () => {
    const getModules = vi
      .fn()
      .mockRejectedValueOnce(new AppError('content.unavailable', 'Lessons could not be loaded.'))
      .mockResolvedValueOnce(modules);
    vi.spyOn(console, 'error').mockImplementation(() => {}); // React logs caught errors.
    await renderModules(fakeDataSource({ getModules }));

    const retry = await screen.findByRole('button', { name: 'Try again (content.unavailable)' });
    await act(async () => fireEvent.click(retry)); // Retrying suspends again, so it needs act() too.

    expect(await screen.findByText('Start here')).toBeInTheDocument();
    expect(getModules).toHaveBeenCalledTimes(2);
    vi.mocked(console.error).mockRestore();
  });

  it('preloads into the same cache the hook reads', async () => {
    const source = fakeDataSource({ getModules: vi.fn().mockResolvedValue(modules) });

    preloadContent(source, content.modules.key, content.modules.fetch);
    await renderModules(source);

    expect(await screen.findByText('Start here')).toBeInTheDocument();
    expect(source.getModules).toHaveBeenCalledOnce();
  });
});
