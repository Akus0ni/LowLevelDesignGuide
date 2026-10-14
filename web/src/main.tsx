import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createHashRouter, RouterProvider } from 'react-router';
import { routes } from './app/routes';
import { createDataSource } from './data/createDataSource';
import { DataSourceProvider } from './data/DataSourceContext';
import './styles/index.css';

// Hash routing: the MAUI WebView has no server to answer deep-link reloads.
const router = createHashRouter(routes);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <DataSourceProvider source={createDataSource()}>
      <RouterProvider router={router} />
    </DataSourceProvider>
  </StrictMode>,
);
