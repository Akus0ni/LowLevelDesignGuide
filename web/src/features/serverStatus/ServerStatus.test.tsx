import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { BridgeDataSource, RAW_MESSAGE_EVENT } from '../../data/bridgeDataSource';
import { DataSourceProvider } from '../../data/DataSourceContext';
import type { DataSource, ServerStatus } from '../../data/types';
import { fakeDataSource, onlineStatus } from '../../test/fakeDataSource';
import { AuthProvider } from '../auth/AuthProvider';
import { RequestAccessForm } from '../auth/forms';
import { ServerOfflineBanner } from './ServerOfflineBanner';
import { ServerStatusProvider } from './ServerStatusProvider';
import { useServerStatus } from './ServerStatusContext';

/** A fake source whose status the test can push, like C# or ntfy would. */
function controllableSource(overrides: Partial<DataSource> = {}) {
  let push: (s: ServerStatus) => void = () => {};
  let sent: (email: string) => void = () => {};
  const source = fakeDataSource({
    onServerStatusChanged: vi.fn((listener) => {
      push = listener;
      return () => {};
    }),
    onAccessRequestSent: vi.fn((listener) => {
      sent = listener;
      return () => {};
    }),
    ...overrides,
  });
  return { source, push: (s: ServerStatus) => act(() => push(s)), sent: (e: string) => act(() => sent(e)) };
}

function CanSync() {
  return <p data-testid="can-sync">{String(useServerStatus().canSync)}</p>;
}

function renderWithStatus(
  source: DataSource,
  children = (
    <>
      <ServerOfflineBanner />
      <CanSync />
    </>
  ),
) {
  return render(
    <DataSourceProvider source={source}>
      <ServerStatusProvider>
        <AuthProvider>{children}</AuthProvider>
      </ServerStatusProvider>
    </DataSourceProvider>,
  );
}

const offline: ServerStatus = { state: 'offline', reason: 'shutdown', since: '2026-10-15T10:00:00Z' };

describe('Server status in the UI', () => {
  it('shows the banner and disables sync while offline, and hides it when back online', async () => {
    const { source, push } = controllableSource();
    renderWithStatus(source);
    const banner = screen.getByRole('status', { hidden: true });
    await vi.waitFor(() => expect(screen.getByTestId('can-sync')).toHaveTextContent('true'));

    push(offline);
    expect(banner).toHaveAttribute('data-visible', 'true');
    expect(banner).toHaveTextContent('Server offline');
    expect(screen.getByTestId('can-sync')).toHaveTextContent('false');

    push(onlineStatus);
    expect(banner).toHaveAttribute('data-visible', 'false');
    expect(banner).toHaveAttribute('aria-hidden', 'true');
    expect(screen.getByTestId('can-sync')).toHaveTextContent('true');
  });

  it('explains a quota stop with the reset date', () => {
    const { source, push } = controllableSource();
    renderWithStatus(source);

    push({
      state: 'offline',
      reason: 'quota',
      since: '2026-10-28T10:00:00Z',
      quotaResetsAt: '2026-11-01T00:00:00Z',
    });

    expect(screen.getByRole('status')).toHaveTextContent(/Server resting until/);
  });

  it('does not flash the banner before the first status arrives', () => {
    const { source } = controllableSource({ getServerStatus: () => new Promise(() => {}) });
    renderWithStatus(source);

    expect(screen.getByRole('status', { hidden: true })).toHaveAttribute('data-visible', 'false');
  });

  it('tells the user a queued request was saved, and toasts when it is sent later', async () => {
    const { source, sent } = controllableSource({
      requestAccess: vi.fn().mockResolvedValue({ queued: true }),
    });
    renderWithStatus(source, <RequestAccessForm />);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.type(screen.getByLabelText('Name'), 'Ada');
    await user.click(screen.getByRole('button', { name: 'Request access' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Request saved, will be sent when the server is online.',
    );
    sent('ada@example.com');
    expect(source.onAccessRequestSent).toHaveBeenCalled();
  });
});

describe('BridgeDataSource events', () => {
  it('routes C# raw messages by type', () => {
    const events = new EventTarget();
    const source = new BridgeDataSource({ InvokeDotNet: vi.fn() }, events);
    const statuses: ServerStatus[] = [];
    const emails: string[] = [];
    source.onServerStatusChanged((s) => statuses.push(s));
    source.onAccessRequestSent((e) => emails.push(e));

    const raw = (message: unknown) =>
      events.dispatchEvent(
        new CustomEvent(RAW_MESSAGE_EVENT, { detail: { message: JSON.stringify(message) } }),
      );
    raw({
      type: 'serverStatus',
      status: { state: 'offline', reason: 'unreachable', since: '2026-10-15T10:00:00Z' },
    });
    raw({ type: 'accessRequestSent', email: 'ada@example.com' });
    raw({ type: 'somethingElse' });
    events.dispatchEvent(new CustomEvent(RAW_MESSAGE_EVENT, { detail: { message: 'not json' } }));

    expect(statuses).toEqual([
      { state: 'offline', reason: 'unreachable', since: '2026-10-15T10:00:00Z', quotaResetsAt: undefined },
    ]);
    expect(emails).toEqual(['ada@example.com']);
  });

  it('reads whether a request was queued', async () => {
    const bridge = { InvokeDotNet: vi.fn().mockResolvedValue({ ok: true, value: { queued: true } }) };
    const source = new BridgeDataSource(bridge, new EventTarget());

    expect(await source.requestAccess({ email: 'a@example.com', name: 'A' })).toEqual({ queued: true });
  });
});
