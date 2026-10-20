import { Drawer } from '@base-ui/react/drawer';
import { useState } from 'react';
import { Link } from 'react-router';
import type { LessonSummary, ModulePicker } from '../../../data/types';
import { cn } from '../../../ui/cn';
import { Icon } from '../../../ui/Icon';
import { LessonIcon } from '../LessonIcon';
import { chevronLeft, chevronRight } from '../navigation';
import { walkPicker } from './pickerWalk';

const compass = ['M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z', 'm15.5 8.5-2 5-5 2 2-5 5-2Z'];

/**
 * "Which pattern?" (spec 006 §3.6): a bottom sheet that asks one question at a time and ends at the pattern(s) that
 * fit, with a one-line why. Back (the button, Esc or Android Back) steps back one question; at the first question it
 * closes. Nothing is saved: the next open starts from the beginning.
 */
export function PatternPicker({ picker, lessons }: { picker: ModulePicker; lessons: LessonSummary[] }) {
  const [open, setOpen] = useState(false);
  const [chosen, setChosen] = useState<number[]>([]);
  const { step, path } = walkPicker(picker, chosen);
  const bySlug = new Map(lessons.map((lesson) => [lesson.slug, lesson]));
  const back = () => setChosen((current) => current.slice(0, -1));

  return (
    <Drawer.Root
      open={open}
      onOpenChange={(next, details) => {
        // Esc (and so Android Back) walks back through the answers before it closes the sheet.
        if (!next && details.reason === 'escape-key' && chosen.length > 0) {
          details.cancel();
          back();
          return;
        }
        setOpen(next);
        if (next) setChosen([]);
      }}
      swipeDirection="down"
    >
      <Drawer.Trigger className="picker-trigger">
        <Icon d={compass} size={18} />
        Which pattern?
      </Drawer.Trigger>
      <Drawer.Portal>
        <Drawer.Backdrop className="sheet-backdrop" />
        <Drawer.Viewport className="fixed inset-0 z-30 flex items-end justify-center">
          <Drawer.Popup className="sheet">
            <div aria-hidden="true" className="mx-auto mt-2.5 mb-1 h-1 w-9 rounded-full bg-fg/15" />
            <div className="flex min-h-11 items-center gap-2 px-3 pt-1">
              {chosen.length > 0 ? (
                <button type="button" onClick={back} className="picker-back">
                  <Icon d={chevronLeft} size={18} />
                  Back
                </button>
              ) : (
                <span className="w-2" />
              )}
              <Drawer.Title className="text-[17px] font-semibold">Which pattern?</Drawer.Title>
            </div>
            <Drawer.Description className="sr-only">
              Answer a few questions about your problem; the guide points you to the pattern that fits.
            </Drawer.Description>
            <Drawer.Content className="max-h-[70dvh] overflow-y-auto overscroll-contain px-5 pt-2 pb-[calc(1.25rem+env(safe-area-inset-bottom,0px))]">
              {path.length > 0 && (
                <p className="picker-path" aria-label="Your answers">
                  {path.map((choice) => choice.answer.label).join(' → ')}
                </p>
              )}
              {step.kind === 'question' ? (
                <div key={step.question.id} className="picker-step">
                  <p className="picker-ask" aria-live="polite">
                    {step.question.ask}
                  </p>
                  <ul className="mt-3 flex flex-col gap-2">
                    {step.question.answers.map((answer, index) => (
                      <li key={answer.label}>
                        <button type="button" className="picker-answer" onClick={() => setChosen([...chosen, index])}>
                          <span className="min-w-0 flex-1">{answer.label}</span>
                          <Icon d={chevronRight} size={18} />
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : (
                <div key="result" className="picker-step">
                  <p className="picker-ask" aria-live="polite">
                    Try {step.answer.picks.map((slug) => bySlug.get(slug)?.title ?? slug).join(' or ')}
                  </p>
                  <p className="mt-1 text-[15px] text-muted">{step.answer.why}</p>
                  <ul className="mt-4 flex flex-col gap-2">
                    {step.answer.picks.map((slug) => {
                      const lesson = bySlug.get(slug);
                      if (!lesson) return null;
                      return (
                        <li key={slug}>
                          <Link
                            to={`/learn/${lesson.moduleSlug}/${lesson.slug}`}
                            className="picker-result"
                            onClick={() => setOpen(false)}
                          >
                            <span aria-hidden="true" className="picker-symbol">
                              {lesson.symbol}
                            </span>
                            <span className="min-w-0 flex-1">
                              <span className="block font-semibold">{lesson.title}</span>
                              {lesson.hook && <span className="block text-[14px] text-muted">{lesson.hook}</span>}
                            </span>
                            <LessonIcon icon={lesson.icon} className="text-[20px]" />
                          </Link>
                        </li>
                      );
                    })}
                  </ul>
                  <button type="button" className={cn('picker-back', 'mt-4')} onClick={() => setChosen([])}>
                    Start over
                  </button>
                </div>
              )}
            </Drawer.Content>
          </Drawer.Popup>
        </Drawer.Viewport>
      </Drawer.Portal>
    </Drawer.Root>
  );
}
