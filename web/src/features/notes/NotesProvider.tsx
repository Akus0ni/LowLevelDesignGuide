import { useEffect, useRef, type ReactNode } from 'react';
import { toast } from 'sonner';
import { useDataSource } from '../../data/DataSourceContext';
import type { DataSource } from '../../data/types';
import { useAuth } from '../auth/AuthContext';
import { NotesStore } from './notesStore';
import { NotesContext } from './useNotes';

const GUEST = 'guest';

/** The store in use for each data source, and whose it is. */
const current = new WeakMap<DataSource, { owner: string; store: NotesStore }>();

function storeFor(source: DataSource, owner: string): NotesStore {
  const entry = current.get(source);
  if (entry?.owner === owner) return entry.store;
  const store = new NotesStore(source);
  current.set(source, { owner, store });
  return store;
}

/**
 * Gives the screens the notes and bookmarks of whoever is signed in, or of the guest (spec 007 §3.8). When that changes,
 * the previous owner's last edits are written and a fresh store loads the new owner's. A write that can't be saved is
 * said once, in a toast, in the app's voice.
 */
export function NotesProvider({ children }: { children: ReactNode }) {
  const source = useDataSource();
  const { state } = useAuth();
  const owner = state.status === 'signedIn' ? state.user.id : GUEST;
  const store = storeFor(source, owner);

  const previous = useRef<NotesStore | null>(null);
  useEffect(() => {
    if (previous.current && previous.current !== store) void previous.current.dispose();
    previous.current = store;
  }, [store]);

  useEffect(() => store.onProblem((problem) => toast.error(problem.message, { id: problem.code })), [store]);

  return <NotesContext value={store}>{children}</NotesContext>;
}
