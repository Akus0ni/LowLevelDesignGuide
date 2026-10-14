import { createContext, useContext, type ReactNode } from 'react';
import type { DataSource } from './types';

const DataSourceContext = createContext<DataSource | null>(null);

export function DataSourceProvider({ source, children }: { source: DataSource; children: ReactNode }) {
  return <DataSourceContext value={source}>{children}</DataSourceContext>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useDataSource(): DataSource {
  const source = useContext(DataSourceContext);
  if (!source) {
    throw new Error('useDataSource must be used inside <DataSourceProvider>');
  }
  return source;
}
