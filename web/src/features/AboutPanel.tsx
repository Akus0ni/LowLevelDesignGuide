import { useCallback, useEffect, useState } from 'react';
import { useDataSource } from '../data/DataSourceContext';
import type { AppInfo } from '../data/types';
import { Button } from '../ui/Button';

type State =
  { status: 'loading' } | { status: 'ready'; info: AppInfo } | { status: 'error'; message: string };

const TRANSPORT_LABEL = { http: 'HTTP API', bridge: 'Native bridge' } as const;

export function AboutPanel() {
  const source = useDataSource();
  const [state, setState] = useState<State>({ status: 'loading' });

  const load = useCallback(() => {
    setState({ status: 'loading' });
    source
      .getInfo()
      .then((info) => setState({ status: 'ready', info }))
      .catch((error: unknown) =>
        setState({ status: 'error', message: error instanceof Error ? error.message : String(error) }),
      );
  }, [source]);

  useEffect(load, [load]);

  return (
    <div className="mt-10 rounded-2xl bg-surface p-5" aria-live="polite">
      <h2 className="text-lg font-semibold">About</h2>
      <dl className="mt-3 grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
        <dt className="text-muted">Transport</dt>
        <dd data-testid="transport">{TRANSPORT_LABEL[source.kind]}</dd>
        {state.status === 'ready' && (
          <>
            <dt className="text-muted">Name</dt>
            <dd data-testid="info-name">{state.info.name}</dd>
            <dt className="text-muted">Version</dt>
            <dd>{state.info.version}</dd>
            <dt className="text-muted">Environment</dt>
            <dd>{state.info.environment}</dd>
          </>
        )}
      </dl>
      {state.status === 'loading' && <p className="mt-3 text-sm text-muted">Loading…</p>}
      {state.status === 'error' && (
        <div className="mt-3 flex items-center gap-3">
          <p className="text-sm text-muted">Couldn&apos;t load info: {state.message}</p>
          <Button size="sm" intent="secondary" onClick={load}>
            Retry
          </Button>
        </div>
      )}
    </div>
  );
}
