import { describe, expect, it, vi } from 'vitest';
import { BridgeDataSource, SEARCH_LIMIT } from './bridgeDataSource';
import { HttpDataSource } from './httpDataSource';

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });
const results = { query: 'srp', expansions: [], total: 0, countsByKind: { lesson: 0, cheatSheet: 0, note: 0 }, hits: [], words: [], suggestions: [] };

describe('search over the bridge', () => {
  it('passes every argument in order, null for the ones not set, and reads PascalCase results', async () => {
    const bridge = {
      InvokeDotNet: vi.fn().mockResolvedValue({
        Ok: true,
        Value: {
          Query: 'srp',
          Expansions: ['single responsibility'],
          Total: 1,
          CountsByKind: { lesson: 1, cheatSheet: 0, note: 0 },
          Hits: [{ Kind: 'lesson', LessonId: 'srp', Snippet: { Text: 'SRP', Marks: [[0, 3]] } }],
          Words: [],
          Suggestions: [],
        },
        Error: null,
      }),
    };

    const found = await new BridgeDataSource(bridge).search({ query: 'srp', kind: 'lesson' });

    expect(bridge.InvokeDotNet).toHaveBeenCalledWith('Search', ['srp', 'lesson', null, null, SEARCH_LIMIT]);
    expect(found.expansions).toEqual(['single responsibility']);
    expect(found.hits[0]).toMatchObject({ kind: 'lesson', lessonId: 'srp', snippet: { text: 'SRP', marks: [[0, 3]] } });
  });
});

describe('search over HTTP (dev API)', () => {
  it('sends the filters as query parameters, without a token for a guest', async () => {
    const fetchFn = vi.fn().mockResolvedValue(json(results));
    const source = new HttpDataSource('', fetchFn);

    await source.search({ query: 'open closed', module: 'oop-solid', lessonId: 'ocp', limit: 8 });

    const [url, init] = fetchFn.mock.calls[0];
    expect(url).toBe('/api/search?q=open+closed&module=oop-solid&lessonId=ocp&limit=8');
    expect(init.headers.Authorization).toBeUndefined();
  });

  it('sends the access token when signed in, so the learner\'s notes are searched too', async () => {
    const session = {
      accessToken: 'access-1',
      accessTokenExpiresAt: '2026-01-01T00:00:00Z',
      user: { id: 'u1', email: 'a@example.com', displayName: 'A', roles: ['Learner'], mustChangePassword: false },
    };
    const fetchFn = vi.fn().mockResolvedValueOnce(json(session)).mockResolvedValue(json(results));
    const source = new HttpDataSource('', fetchFn);
    await source.login('a@example.com', 'pw');

    await source.search({ query: 'gotcha' });

    const [url, init] = fetchFn.mock.calls[fetchFn.mock.calls.length - 1];
    expect(url).toBe('/api/search?q=gotcha');
    expect(init.headers.Authorization).toBe('Bearer access-1');
  });
});
