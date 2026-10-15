import { describe, expect, it, vi } from 'vitest';
import { HttpDataSource } from '../httpDataSource';
import { AppError, type ServerStatus } from '../types';
import { AccessRequestOutbox } from './accessRequestOutbox';
import { ServerStatusTracker } from './serverStatusTracker';

function memoryStore() {
  const data = new Map<string, string>();
  return {
    getItem: (k: string) => data.get(k) ?? null,
    setItem: (k: string, v: string) => void data.set(k, v),
    removeItem: (k: string) => void data.delete(k),
  };
}

const request = { email: 'ada@example.com', name: 'Ada' };
const accepted = () => new Response(JSON.stringify({ message: 'ok' }), { status: 202 });

function setup(fetchFn: ReturnType<typeof vi.fn>, initial: 'unknown' | 'online' = 'online') {
  const tracker = new ServerStatusTracker(Date.now, initial);
  const outbox = new AccessRequestOutbox(memoryStore());
  const source = new HttpDataSource('', fetchFn as unknown as typeof fetch, {
    tracker,
    outbox,
    connectStatus: () => () => {},
  });
  return { source, tracker, outbox };
}

describe('HttpDataSource server status', () => {
  it('marks the server offline when a call cannot reach it, and online when one succeeds', async () => {
    const fetchFn = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ name: 'LLD Guide', version: '1', environment: 'x' }), { status: 200 }),
      );
    const { source } = setup(fetchFn);

    await expect(source.getInfo()).rejects.toMatchObject({ code: 'network.unavailable' });
    expect((await source.getServerStatus()).state).toBe('offline');

    await source.getInfo();
    expect((await source.getServerStatus()).state).toBe('online');
  });

  it('treats tunnel gateway errors as offline', async () => {
    const { source } = setup(vi.fn().mockResolvedValue(new Response('bad gateway', { status: 502 })));

    await expect(source.getInfo()).rejects.toBeInstanceOf(AppError);

    expect((await source.getServerStatus()).state).toBe('offline');
  });

  it('saves an access request while offline and sends it when the server is back', async () => {
    const fetchFn = vi.fn().mockResolvedValue(accepted());
    const { source, tracker, outbox } = setup(fetchFn);
    tracker.reportCallFailed(); // offline
    const sent = vi.fn();
    source.onAccessRequestSent(sent);

    const outcome = await source.requestAccess(request);

    expect(outcome).toEqual({ queued: true });
    expect(fetchFn).not.toHaveBeenCalled();
    expect(outbox.pending).toEqual([request]);

    tracker.reportCallSucceeded(); // online: flush
    await vi.waitFor(() => expect(sent).toHaveBeenCalledWith('ada@example.com'));
    expect(outbox.pending).toEqual([]);
  });

  it('queues the request when sending fails at the network level', async () => {
    const { source, outbox } = setup(vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    expect(await source.requestAccess(request)).toEqual({ queued: true });
    expect(outbox.pending).toHaveLength(1);
  });

  it('does not queue a request the server refused', async () => {
    const refused = new Response(JSON.stringify({ code: 'validation.email', title: 'Bad email' }), {
      status: 400,
    });
    const { source, outbox } = setup(vi.fn().mockResolvedValue(refused));

    await expect(source.requestAccess(request)).rejects.toMatchObject({ code: 'validation.email' });
    expect(outbox.pending).toEqual([]);
  });

  it('keeps one saved request per email, the latest one', () => {
    const outbox = new AccessRequestOutbox(memoryStore());
    outbox.enqueue({ ...request, name: 'Old' });
    outbox.enqueue({ ...request, email: 'ADA@example.com', name: 'New' });

    expect(outbox.pending).toEqual([{ ...request, email: 'ADA@example.com', name: 'New' }]);
  });

  it('starts listening with the first subscriber and stops with the last', () => {
    const stop = vi.fn();
    const connect = vi.fn().mockReturnValue(stop);
    const source = new HttpDataSource('', vi.fn(), {
      connectStatus: connect,
      outbox: new AccessRequestOutbox(memoryStore()),
    });
    const seen: ServerStatus[] = [];

    const first = source.onServerStatusChanged((s) => seen.push(s));
    const second = source.onServerStatusChanged(() => {});
    expect(connect).toHaveBeenCalledTimes(1);

    first();
    expect(stop).not.toHaveBeenCalled();
    second();
    expect(stop).toHaveBeenCalledTimes(1);
  });
});
