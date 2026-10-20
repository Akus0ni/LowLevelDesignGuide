import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createHashRouter, RouterProvider } from 'react-router';
import { Toaster } from 'sonner';
import { installBackHandler } from './app/hardwareBack';
import { routes, warmUpReader } from './app/routes';
import { createDataSource } from './data/createDataSource';
import { warmUpHighlighter } from './features/content/markdown/highlighter';
import { DataSourceProvider } from './data/DataSourceContext';
import { AuthProvider } from './features/auth/AuthProvider';
import { NotesProvider } from './features/notes/NotesProvider';
import { ProgressProvider } from './features/progress/ProgressProvider';
import { ServerStatusProvider } from './features/serverStatus/ServerStatusProvider';
import '@fontsource-variable/bricolage-grotesque/opsz.css';
import '@fontsource-variable/jetbrains-mono';
import './styles/index.css';

// Hash routing: the MAUI WebView has no server to answer deep-link reloads.
const router = createHashRouter(routes);
installBackHandler();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <DataSourceProvider source={createDataSource()}>
      <ServerStatusProvider>
        <AuthProvider>
          <ProgressProvider>
            <NotesProvider>
              <RouterProvider router={router} />
            </NotesProvider>
          </ProgressProvider>
        </AuthProvider>
      </ServerStatusProvider>
    </DataSourceProvider>
    {/* One Toaster at the root (ask-sonner); keep clear of the notch on phones. */}
    <Toaster
      theme="system"
      position="top-center"
      mobileOffset={{ top: 'calc(env(safe-area-inset-top, 0px) + 12px)' }}
    />
  </StrictMode>,
);

// After the first screen: load the reader and the highlighter while the app is idle (spec 003, A10).
warmUpReader();
warmUpHighlighter();
