import { Icon } from '../../../ui/Icon';
import { stripButton } from './stripButton';
import type { ActivitySteps, StateDiagram } from './useUmlDiagrams';
import { PSEUDO } from './umlSource';

const previousIcon = ['m15 6-6 6 6 6'];
const arrowIcon = ['M5 12h14', 'm13 6 6 6-6 6'];

// ---- State diagrams -------------------------------------------------------------------------------------------

/** Top strip: the tap hint, "run", or (while running) back, where the run is, and stop. */
export function StateStrip({ machine }: { machine: StateDiagram }) {
  if (machine.run) {
    return (
      <>
        <button
          type="button"
          aria-label="Undo the last event"
          className={stripButton}
          disabled={machine.steps <= 1}
          onClick={machine.back}
        >
          <Icon d={previousIcon} size={14} />
        </button>
        <span className="max-w-[9rem] truncate px-1 text-accent">
          {machine.run.state === PSEUDO ? 'done' : machine.run.state}
        </span>
        <button type="button" className={stripButton} onClick={machine.stop}>
          stop
        </button>
      </>
    );
  }
  return (
    <>
      {machine.focused ? (
        <>
          <span className="max-w-[9rem] truncate px-1 text-accent">
            {machine.focused} · {machine.linked} link{machine.linked === 1 ? '' : 's'}
          </span>
          <button type="button" className={stripButton} onClick={machine.clearFocus}>
            clear
          </button>
        </>
      ) : (
        <span className="hidden px-2 sm:inline">tap a state to trace it</span>
      )}
      {machine.canRun && (
        <button type="button" className={stripButton} onClick={machine.start}>
          run
        </button>
      )}
    </>
  );
}

/** Under the diagram while running: one button per event the current state accepts, or "run again" at the end. */
export function StateRunBar({ machine }: { machine: StateDiagram }) {
  if (!machine.run) return null;
  return (
    <div className="diagram-run-bar" role="group" aria-label="Events">
      {machine.done ? (
        <>
          <span className="diagram-run-note">Final state reached.</span>
          <button type="button" className="diagram-run-button" onClick={machine.start}>
            run again
          </button>
        </>
      ) : (
        machine.events.map((transition) => (
          <button
            key={transition.index}
            type="button"
            className="diagram-run-button"
            onClick={() => machine.fire(transition)}
          >
            {transition.event ?? (transition.to === PSEUDO ? 'finish' : 'next')}
            <Icon d={arrowIcon} size={14} />
          </button>
        ))
      )}
    </div>
  );
}

// ---- Activity diagrams ----------------------------------------------------------------------------------------

export function ActivityStrip({ walk }: { walk: ActivitySteps }) {
  if (!walk.stepping) {
    return (
      <button type="button" className={stripButton} onClick={walk.start}>
        step through
      </button>
    );
  }
  return (
    <>
      <button
        type="button"
        aria-label="Previous step"
        className={stripButton}
        disabled={walk.index <= 1}
        onClick={walk.back}
      >
        <Icon d={previousIcon} size={14} />
      </button>
      <span className="min-w-[3.25rem] text-center tabular-nums text-fg">step {walk.index}</span>
      <button type="button" className={stripButton} onClick={walk.stop}>
        all
      </button>
    </>
  );
}

/** Under the diagram while stepping: Next, one button per branch at a decision, or "again" at the end. */
export function ActivityBar({ walk }: { walk: ActivitySteps }) {
  if (!walk.stepping) return null;
  return (
    <div className="diagram-run-bar" role="group" aria-label="Next step">
      {walk.done ? (
        <>
          <span className="diagram-run-note">The flow ends here.</span>
          <button type="button" className="diagram-run-button" onClick={walk.start}>
            again
          </button>
        </>
      ) : walk.choices.length > 0 ? (
        walk.choices.map((choice) => (
          <button
            key={choice.edge}
            type="button"
            className="diagram-run-button"
            onClick={() => walk.next(choice.edge)}
          >
            {choice.label}
            <Icon d={arrowIcon} size={14} />
          </button>
        ))
      ) : (
        <button type="button" className="diagram-run-button" onClick={() => walk.next()}>
          next
          <Icon d={arrowIcon} size={14} />
        </button>
      )}
    </div>
  );
}
