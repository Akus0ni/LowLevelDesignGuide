import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import type { ModuleDetail } from '../../data/types';
import { moduleCelebrationId } from './celebrations';
import { countDone } from './progressMath';
import { useProgress, useProgressStore } from './useProgress';

/**
 * True only the first time this moment is shown to this owner (spec 005 §3.8); remembered at once, so coming back to
 * the page doesn't replay it.
 */
export function useFirstShow(id: string, active: boolean): boolean {
  const store = useProgressStore();
  const [fresh] = useState(() => active && !store.celebrations.has(id));
  useEffect(() => {
    if (fresh) store.celebrations.remember(id);
  }, [fresh, store, id]);
  return fresh;
}

/**
 * Finishing a module's last lesson from the lesson page: a toast, once, since the module page (where the stamp is)
 * isn't on screen. The stamp still presses in the next time the module page is opened.
 */
export function useModuleCompleteToast(module: ModuleDetail): void {
  const store = useProgressStore();
  const { byId, freshlyDone } = useProgress();
  const count = countDone(byId, module.lessons);
  const complete = count.total > 0 && count.done === count.total;
  const justFinished = module.lessons.some((lesson) => freshlyDone.has(lesson.id));
  const id = `${moduleCelebrationId(module.slug)}:toast`;

  useEffect(() => {
    if (!complete || !justFinished || store.celebrations.has(id)) return;
    store.celebrations.remember(id);
    toast.success(`${module.title} complete`, { description: `All ${count.total} lessons done.` });
  }, [complete, justFinished, store, id, module.title, count.total]);
}
