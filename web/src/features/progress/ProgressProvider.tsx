import { useEffect, useRef, type ReactNode } from 'react';
import { useDataSource } from '../../data/DataSourceContext';
import type { DataSource } from '../../data/types';
import { useAuth } from '../auth/AuthContext';
import { ProgressStore } from './progressStore';
import { ProgressContext } from './useProgress';

const GUEST = 'guest';

/** The store in use for each data source, and whose it is. */
const current = new WeakMap<DataSource, { owner: string; store: ProgressStore }>();

function storeFor(source: DataSource, owner: string): ProgressStore {
  const entry = current.get(source);
  if (entry?.owner === owner) return entry.store;
  const store = new ProgressStore(source, { owner });
  current.set(source, { owner, store });
  return store;
}

/**
 * Gives the screens the progress of whoever is signed in, or of the guest (spec 005 §3.9). When that changes (sign in,
 * sign out), the previous owner's last changes are written and a fresh store loads the new owner's progress.
 */
export function ProgressProvider({ children }: { children: ReactNode }) {
  const source = useDataSource();
  const { state } = useAuth();
  const owner = state.status === 'signedIn' ? state.user.id : GUEST;
  const store = storeFor(source, owner);

  const previous = useRef<ProgressStore | null>(null);
  useEffect(() => {
    if (previous.current && previous.current !== store) void previous.current.dispose();
    previous.current = store;
  }, [store]);

  return <ProgressContext value={store}>{children}</ProgressContext>;
}
