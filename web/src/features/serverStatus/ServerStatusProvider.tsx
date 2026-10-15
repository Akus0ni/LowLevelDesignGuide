import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { toast } from 'sonner';
import { useDataSource } from '../../data/DataSourceContext';
import type { ServerStatus } from '../../data/types';
import { ServerStatusContext, type ServerStatusValue } from './ServerStatusContext';

const INITIAL: ServerStatus = { state: 'unknown', reason: 'no_signal', since: new Date(0).toISOString() };

/** Keeps the server status current for the whole app (Observer over the DataSource's status events). */
export function ServerStatusProvider({ children }: { children: ReactNode }) {
  const source = useDataSource();
  const [status, setStatus] = useState<ServerStatus>(INITIAL);

  useEffect(() => {
    let heardEvent = false;
    let active = true;
    const stopStatus = source.onServerStatusChanged((next) => {
      heardEvent = true;
      setStatus(next);
    });
    const stopSent = source.onAccessRequestSent((email) =>
      toast.success('Access request sent', {
        description: `Your saved request for ${email} reached the admin.`,
      }),
    );
    // A live event is newer than this snapshot, so it wins if both arrive.
    source
      .getServerStatus()
      .then((current) => active && !heardEvent && setStatus(current))
      .catch(() => {});

    return () => {
      active = false;
      stopStatus();
      stopSent();
    };
  }, [source]);

  const value = useMemo<ServerStatusValue>(() => ({ status, canSync: status.state === 'online' }), [status]);
  return <ServerStatusContext value={value}>{children}</ServerStatusContext>;
}
