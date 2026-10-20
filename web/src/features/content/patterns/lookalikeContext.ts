import { createContext } from 'react';
import type { ModuleDetail } from '../../../data/types';

/** The lesson being read and its module, for the look-alike strip (spec 006 §3.7); null outside a lesson. */
export const LookalikeContext = createContext<{ module: ModuleDetail; lessonSlug: string } | null>(null);
