import type { ReactNode } from 'react';
import { DataSourceProvider } from '../data/DataSourceContext';
import type { DataSource } from '../data/types';
import { AuthProvider } from '../features/auth/AuthProvider';
import { ProgressProvider } from '../features/progress/ProgressProvider';
import { ServerStatusProvider } from '../features/serverStatus/ServerStatusProvider';

/** The providers main.tsx puts around the app, for screen tests. */
export function AppProviders({ source, children }: { source: DataSource; children: ReactNode }) {
  return (
    <DataSourceProvider source={source}>
      <ServerStatusProvider>
        <AuthProvider>
          <ProgressProvider>{children}</ProgressProvider>
        </AuthProvider>
      </ServerStatusProvider>
    </DataSourceProvider>
  );
}
