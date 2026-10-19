import { Drawer } from '@base-ui/react/drawer';
import { useState } from 'react';
import { toast } from 'sonner';
import type { ModuleDetail } from '../../data/types';
import { cn } from '../../ui/cn';
import { reset } from './lessonProgress';
import { useProgressStore } from './useProgress';

const EVERYTHING = '*';

/**
 * Reset progress (spec 005 §3.7): the only destructive action in progress, so the only one that asks first. A bottom
 * sheet (Back and Esc close it) where the learner picks a module or everything; the button names exactly that.
 */
export function ResetSheet({ modules }: { modules: readonly ModuleDetail[] }) {
  const store = useProgressStore();
  const [open, setOpen] = useState(false);
  const [scope, setScope] = useState<string | null>(null);
  const chosen = scope === EVERYTHING ? null : modules.find((m) => m.slug === scope);
  const name = scope === EVERYTHING ? 'everything' : chosen?.title;
  const lessons = scope === EVERYTHING ? modules.flatMap((m) => m.lessons) : (chosen?.lessons ?? []);

  const confirm = () => {
    store.changeAll(
      lessons.map((lesson) => lesson.id),
      reset,
    );
    toast.success(scope === EVERYTHING ? 'All progress reset' : `${chosen?.title} progress reset`);
    setOpen(false);
  };

  return (
    <Drawer.Root
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) setScope(null);
      }}
      swipeDirection="down"
    >
      <Drawer.Trigger
        className={cn(
          'inline-flex h-11 items-center rounded-full border border-rule bg-surface px-4 text-[15px] font-medium select-none',
          'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
          'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        )}
      >
        Reset progress
      </Drawer.Trigger>
      <Drawer.Portal>
        <Drawer.Backdrop className="sheet-backdrop" />
        <Drawer.Viewport className="fixed inset-0 z-30 flex items-end justify-center">
          <Drawer.Popup className="sheet">
            <div aria-hidden="true" className="mx-auto mt-2.5 mb-1 h-1 w-9 rounded-full bg-fg/15" />
            <Drawer.Title className="px-5 pt-2 text-[19px] font-semibold">Reset progress</Drawer.Title>
            <Drawer.Description className="px-5 pt-1 text-[15px] text-muted">
              Clears done marks, read sections, reading positions and revised dates. Study days are kept.
            </Drawer.Description>
            <Drawer.Content className="max-h-[60dvh] overflow-y-auto overscroll-contain px-3 pt-3 pb-[calc(1rem+env(safe-area-inset-bottom,0px))]">
              <div role="radiogroup" aria-label="What to reset" className="flex flex-col">
                {[...modules.map((m) => ({ value: m.slug, label: m.title })), { value: EVERYTHING, label: 'Everything' }].map(
                  (option) => (
                    <button
                      key={option.value}
                      type="button"
                      role="radio"
                      aria-checked={scope === option.value}
                      onClick={() => setScope(option.value)}
                      className={cn(
                        'flex min-h-11 items-center gap-3 rounded-md px-2 text-left text-[15px] select-none',
                        'transition-colors duration-150 ease-out active:bg-fg/[0.05] hover:bg-fg/[0.025]',
                        'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-accent',
                      )}
                    >
                      <span
                        aria-hidden="true"
                        className={cn(
                          'grid size-5 shrink-0 place-items-center rounded-full border-[1.5px]',
                          scope === option.value ? 'border-fg' : 'border-rule',
                        )}
                      >
                        {scope === option.value && <span className="size-2.5 rounded-full bg-fg" />}
                      </span>
                      {option.label}
                    </button>
                  ),
                )}
              </div>
              <div className="mt-4 flex flex-col gap-2 px-2 sm:flex-row-reverse">
                <button
                  type="button"
                  disabled={scope === null}
                  onClick={confirm}
                  className={cn(
                    'inline-flex h-12 items-center justify-center rounded-full bg-[var(--tone-rose)] px-6 text-[15px] font-semibold text-white select-none',
                    'transition-[transform,opacity] duration-[120ms] ease-out active:scale-[0.97] disabled:opacity-40 disabled:active:scale-100',
                    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
                  )}
                >
                  {name ? `Reset ${name}` : 'Choose what to reset'}
                </button>
                <Drawer.Close className="inline-flex h-12 items-center justify-center rounded-full px-6 text-[15px] font-medium text-muted select-none hover:text-fg focus-visible:outline-2 focus-visible:outline-accent">
                  Cancel
                </Drawer.Close>
              </div>
            </Drawer.Content>
          </Drawer.Popup>
        </Drawer.Viewport>
      </Drawer.Portal>
    </Drawer.Root>
  );
}
