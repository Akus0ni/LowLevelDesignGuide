import { Navigate, type RouteObject } from 'react-router';
import { AboutPanel } from '../features/AboutPanel';
import { ComingSoon } from '../features/ComingSoon';
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
        element: (
          <ComingSoon section={section}>{section.path === 'profile' ? <AboutPanel /> : null}</ComingSoon>
        ),
      })),
    ],
  },
];
