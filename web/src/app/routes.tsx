import { Navigate, type RouteObject } from 'react-router';
import { ComingSoon } from '../features/ComingSoon';
import { ProfilePage } from '../features/auth/screens';
import { AppShell } from './AppShell';
import { SECTIONS } from './sections';

export const routes: RouteObject[] = [
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="learn" replace /> },
      ...SECTIONS.map((section) => ({
        path: section.path,
        element: section.path === 'profile' ? <ProfilePage /> : <ComingSoon section={section} />,
      })),
    ],
  },
];
