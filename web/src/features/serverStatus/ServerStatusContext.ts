import { createContext, useContext } from 'react';
import type { ServerStatus } from '../../data/types';

export interface ServerStatusValue {
  status: ServerStatus;
  /** Sync (spec 010) is allowed only while the server is known to be online, for everyone. */
  canSync: boolean;
}

export const ServerStatusContext = createContext<ServerStatusValue | null>(null);

export function useServerStatus(): ServerStatusValue {
  const value = useContext(ServerStatusContext);
  if (!value) throw new Error('useServerStatus must be used inside <ServerStatusProvider>');
  return value;
}

/** Shown to the user: hidden while online, and during the first moment after launch (no news yet). */
export function isBannerVisible(status: ServerStatus): boolean {
  return status.state === 'offline' || (status.state === 'unknown' && status.reason !== 'no_signal');
}
