import { Navigate, type RouteObject } from 'react-router';
import { ComingSoon } from '../features/ComingSoon';
import { ProfilePage } from '../features/auth/screens';
import { LearnPage } from '../features/content/LearnPage';
import { ModulePage } from '../features/content/ModulePage';
import { AppShell } from './AppShell';
import { SECTIONS } from './sections';

/** Sections that have their real screens; the rest show a "coming in spec N" placeholder. */
const BUILT: Record<string, RouteObject> = {
  learn: {
    path: 'learn',
    children: [
      { index: true, element: <LearnPage /> },
      { path: ':module', element: <ModulePage /> },
    ],
  },
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
