import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createHashRouter, RouterProvider } from 'react-router';
import { Toaster } from 'sonner';
import { routes } from './app/routes';
import { createDataSource } from './data/createDataSource';
import { DataSourceProvider } from './data/DataSourceContext';
import { AuthProvider } from './features/auth/AuthProvider';
import './styles/index.css';

// Hash routing: the MAUI WebView has no server to answer deep-link reloads.
const router = createHashRouter(routes);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <DataSourceProvider source={createDataSource()}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </DataSourceProvider>
    {/* One Toaster at the root (ask-sonner); keep clear of the notch on phones. */}
    <Toaster
      theme="system"
      position="top-center"
      mobileOffset={{ top: 'calc(env(safe-area-inset-top, 0px) + 12px)' }}
    />
  </StrictMode>,
);
