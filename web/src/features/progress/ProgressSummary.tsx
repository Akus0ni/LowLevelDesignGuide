import { useModuleDetails, useModules } from '../../data/useContent';
import { ContentBoundary } from '../content/ContentBoundary';
import { ActivityStrip } from './ActivityStrip';
import { ModuleCount } from './ModuleProgress';
import { countDone } from './progressMath';
import { ResetSheet } from './ResetSheet';
import { useProgress } from './useProgress';

/** Profile's progress section (spec 005 §3.3): the overall count, this week, each module's count, and reset. */
export function ProgressSummary() {
  return (
    <section aria-labelledby="progress-heading" className="mt-10 max-w-md">
      <h2 id="progress-heading" className="text-lg font-semibold">
        Progress
      </h2>
      <ContentBoundary fallback={<p className="mt-3 text-muted">Loading…</p>}>
        <Summary />
      </ContentBoundary>
    </section>
  );
}

function Summary() {
  const modules = useModuleDetails(useModules().map((module) => module.slug));
  const { byId, activityDays } = useProgress();
  const overall = countDone(byId, modules.flatMap((module) => module.lessons));

  return (
    <div className="mt-3 flex flex-col gap-5">
      <div className="rounded-2xl bg-surface p-5">
        <p className="font-mono text-[12px] tracking-[0.08em] text-muted">lessons done</p>
        <p className="mt-1 text-[28px] leading-none font-semibold tabular-nums">
          {overall.done} <span className="text-[17px] font-normal text-muted">of {overall.total}</span>
        </p>
        <div className="mt-5">
          <ActivityStrip activityDays={activityDays} />
        </div>
      </div>
      <ul aria-label="By module" className="overflow-hidden rounded-lg border border-rule bg-surface">
        {modules.map((module) => (
          <li
            key={module.slug}
            className="flex items-baseline justify-between gap-3 border-t border-rule px-4 py-3 font-mono text-[12px] first:border-t-0"
          >
            <span className="min-w-0 truncate font-sans text-[15px]">{module.title}</span>
            <ModuleCount count={countDone(byId, module.lessons)} />
          </li>
        ))}
      </ul>
      <div>
        <ResetSheet modules={modules} />
      </div>
    </div>
  );
}
