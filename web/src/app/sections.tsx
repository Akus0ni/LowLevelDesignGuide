import type { ReactNode } from 'react';
import { Icon } from '../ui/Icon';

export interface Section {
  path: string;
  label: string;
  /** Which spec delivers the real screen; shown on the placeholder (none once it exists). */
  comingIn?: string;
  summary: string;
  icon: ReactNode;
}

export const SECTIONS: Section[] = [
  {
    path: 'learn',
    label: 'Learn',
    summary: 'Lessons on OOP, SOLID, design patterns, UML and case studies.',
    icon: <Icon d={['M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2V5Z', 'M4 19a2 2 0 0 1 2-2h13']} />,
  },
  {
    path: 'revise',
    label: 'Revise',
    summary: 'One-screen cheat sheets for quick revision before an interview.',
    icon: <Icon d={['M13 2 4 14h7l-1 8 9-12h-7l1-8Z']} />,
  },
  {
    path: 'search',
    label: 'Search',
    comingIn: 'Spec 009',
    summary: 'Search across lessons, cheat sheets and your notes.',
    icon: <Icon d={['M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z', 'm20 20-3.5-3.5']} />,
  },
  {
    path: 'notes',
    label: 'Notes',
    summary: 'Your notes and bookmarks, saved on this device and synced when online.',
    icon: <Icon d={['M4 20h4L19 9l-4-4L4 16v4Z', 'm13 7 4 4']} />,
  },
  {
    path: 'profile',
    label: 'Profile',
    summary: 'Sign in, request access and manage your account.',
    icon: <Icon d={['M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M4 20a8 8 0 0 1 16 0']} />,
  },
];
