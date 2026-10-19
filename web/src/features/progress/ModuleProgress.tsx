import type { Count } from './progressMath';

/**
 * Lessons done, written like a UML multiplicity (`7/15`) in the box's corner (spec 005 §3.11). The label reads it out.
 */
export function ModuleCount({ count }: { count: Count }) {
  return (
    <span className="text-[12px] text-fg tabular-nums" aria-label={`${count.done} of ${count.total} lessons done`}>
      {count.done}/{count.total}
    </span>
  );
}

/** Fine blueprint hatching across the «module» compartment, as far as the module is done: the drawing being inked. */
export function ModuleHatch({ count }: { count: Count }) {
  if (count.done === 0 || count.total === 0) return null;
  const undone = 100 - (count.done / count.total) * 100;
  return (
    <span
      aria-hidden="true"
      className="module-hatch pointer-events-none absolute inset-0"
      style={{ clipPath: `inset(0 ${undone}% 0 0)` }}
    />
  );
}

/** «complete», pressed onto a finished module like the REMEMBER stamp: ink, double frame, slightly askew. */
export function CompleteStamp({ fresh }: { fresh: boolean }) {
  return (
    <span className="complete-stamp" data-fresh={fresh || undefined}>
      «complete»
    </span>
  );
}
