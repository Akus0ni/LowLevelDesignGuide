import { describe, expect, it, vi } from 'vitest';
import { HttpDataSource } from './httpDataSource';
import { AppError } from './types';

const user = {
  id: 'u1',
  email: 'a@example.com',
  displayName: 'A',
  roles: ['Learner'],
  mustChangePassword: false,
};
const session = (token: string) => ({
  accessToken: token,
  accessTokenExpiresAt: '2026-01-01T00:00:00Z',
  user,
});
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });
const problem = (code: string, status: number) => json({ title: code, code }, status);

describe('HttpDataSource auth', () => {
  it('identifies itself as the web client and keeps the access token in memory', async () => {
    const fetchFn = vi
      .fn()
      .mockResolvedValueOnce(json(session('access-1')))
      .mockResolvedValueOnce(json({ sessionsRevoked: 1 }));
    const source = new HttpDataSource('', fetchFn);

    await source.login('a@example.com', 'pw');
    await source.logoutAll();

    const [, loginInit] = fetchFn.mock.calls[0];
    expect(loginInit.headers['X-Lld-Client']).toBe('web');
    expect(loginInit.credentials).toBe('same-origin');
    const [, logoutAllInit] = fetchFn.mock.calls[1];
    expect(logoutAllInit.headers.Authorization).toBe('Bearer access-1');
  });

  it('refreshes once on 401 and retries the request', async () => {
    const fetchFn = vi
      .fn()
      .mockResolvedValueOnce(json(session('expired')))
      .mockResolvedValueOnce(problem('auth.session_invalid', 401))
      .mockResolvedValueOnce(json(session('fresh')))
      .mockResolvedValueOnce(json(session('after-change')));
    const source = new HttpDataSource('', fetchFn);
    await source.login('a@example.com', 'pw');

    await source.changePassword('old', 'a-new-long-password');

    expect(fetchFn.mock.calls.map(([url]) => url)).toEqual([
      '/api/auth/login',
      '/api/auth/change-password',
      '/api/auth/refresh',
      '/api/auth/change-password',
    ]);
    expect(fetchFn.mock.calls[3][1].headers.Authorization).toBe('Bearer fresh');
  });

  it('restoreSession returns null when there is no valid refresh cookie', async () => {
    const source = new HttpDataSource('', vi.fn().mockResolvedValue(problem('auth.session_invalid', 401)));

    await expect(source.restoreSession()).resolves.toBeNull();
  });

  it('shares one refresh between concurrent callers', async () => {
    const fetchFn = vi.fn().mockResolvedValue(json(session('one')));
    const source = new HttpDataSource('', fetchFn);

    await Promise.all([source.restoreSession(), source.restoreSession()]);

    expect(fetchFn).toHaveBeenCalledTimes(1);
  });

  it('turns problem responses into AppError with the server code', async () => {
    const source = new HttpDataSource('', vi.fn().mockResolvedValue(problem('auth.locked_out', 423)));

    const error = await source.login('a@example.com', 'pw').catch((e: unknown) => e);

    expect(error).toBeInstanceOf(AppError);
    expect(error).toMatchObject({ code: 'auth.locked_out', status: 423 });
  });
});
