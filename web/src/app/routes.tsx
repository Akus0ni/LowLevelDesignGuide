import { Navigate, type RouteObject } from 'react-router';
import { ComingSoon } from '../features/ComingSoon';
import { ProfilePage } from '../features/auth/screens';
import { LearnPage } from '../features/content/LearnPage';
import { ModulePage } from '../features/content/ModulePage';
import { RevisePage } from '../features/content/RevisePage';
import { NotesPage } from '../features/notes/NotesPage';
import { AppShell } from './AppShell';
import { SECTIONS } from './sections';

/** The reader's code; imported on first lesson open, or earlier by `warmUpReader`. */
const loadReader = () => import('../features/content/LessonPage');
const loadCheatSheet = () => import('../features/content/CheatSheetPage');
const loadCompare = () => import('../features/content/patterns/LookalikeCompare');

/** Starts loading the reader when the browser is idle, so the first lesson opens without waiting for code. */
export function warmUpReader(): void {
  const start = () => void Promise.all([loadReader(), loadCheatSheet()]).catch(() => {});
  if ('requestIdleCallback' in window) window.requestIdleCallback(start, { timeout: 3000 });
  else setTimeout(start, 1);
}

/** Route options the shell reads (see AppShell). */
export interface RouteHandle {
  wide?: boolean;
}

/** Sections that have their real screens; the rest show a "coming in spec N" placeholder. */
const BUILT: Record<string, RouteObject> = {
  learn: {
    path: 'learn',
    children: [
      { index: true, element: <LearnPage /> },
      { path: ':module', element: <ModulePage /> },
      // The reader needs room for the outline column on desktop.
      {
        path: ':module/:lesson',
        // The reader (Markdown, highlighting) loads on demand and is warmed while the app is idle (main.tsx).
        lazy: async () => ({ Component: (await loadReader()).LessonPage }),
        handle: { wide: true } satisfies RouteHandle,
      },
      // Look-alike compare (spec 006 §3.7): three segments, so it never clashes with a lesson route.
      {
        path: ':module/compare/:pair',
        lazy: async () => ({ Component: (await loadCompare()).LookalikeCompare }),
        handle: { wide: true } satisfies RouteHandle,
      },
    ],
  },
  revise: {
    path: 'revise',
    children: [
      { index: true, element: <RevisePage /> },
      { path: ':module/:lesson', lazy: async () => ({ Component: (await loadCheatSheet()).CheatSheetPage }) },
    ],
  },
  notes: { path: 'notes', element: <NotesPage /> },
  profile: { path: 'profile', element: <ProfilePage /> },
};

export const routes: RouteObject[] = [
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="learn" replace /> },
      ...SECTIONS.map(
        (section) => BUILT[section.path] ?? { path: section.path, element: <ComingSoon section={section} /> },
      ),
    ],
  },
];
