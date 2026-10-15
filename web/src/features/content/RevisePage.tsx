import { Link, useSearchParams } from 'react-router';
import type { CheatSheetSummary } from '../../data/types';
import { content, useCheatSheets } from '../../data/useContent';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { ContentBoundary } from './ContentBoundary';
import { chevronRight, plural, usePreload } from './navigation';
import { ListSkeleton } from './Skeletons';

const boltIcon = ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'];

/** Revise: every cheat sheet, grouped by module, filterable by module (spec 003 §3.5, PRD F3). */
export function RevisePage() {
  return (
    <section className="pt-8 md:pt-12">
      <title>Revise · LLD Guide</title>
      <h1 className="text-[34px] font-semibold md:text-5xl">Revise</h1>
      <p className="mt-2 max-w-prose text-muted">
        One-screen cheat sheets for quick revision before an interview.
      </p>
      <ContentBoundary fallback={<ListSkeleton />}>
        <CheatSheetList />
      </ContentBoundary>
    </section>
  );
}

interface Group {
  slug: string;
  title: string;
  sheets: CheatSheetSummary[];
}

function groupByModule(sheets: CheatSheetSummary[]): Group[] {
  const groups: Group[] = [];
  for (const sheet of sheets) {
    const group = groups.find((g) => g.slug === sheet.moduleSlug);
    if (group) group.sheets.push(sheet);
    else groups.push({ slug: sheet.moduleSlug, title: sheet.moduleTitle, sheets: [sheet] });
  }
  return groups;
}

function CheatSheetList() {
  // One request; filtering is instant on the client, and `?module=` keeps the filter across Back.
  const sheets = useCheatSheets();
  const [params, setParams] = useSearchParams();
  const groups = groupByModule(sheets);
  const selected = groups.some((g) => g.slug === params.get('module')) ? params.get('module') : null;
  const shown = selected ? groups.filter((g) => g.slug === selected) : groups;

  if (groups.length === 0) {
    return <p className="mt-8 text-muted">Cheat sheets appear here as lessons are added.</p>;
  }

  const choose = (slug: string | null) => setParams(slug ? { module: slug } : {}, { replace: true });

  return (
    <>
      <div
        role="group"
        aria-label="Filter by module"
        className="-mx-5 mt-6 flex gap-2 overflow-x-auto overscroll-x-contain px-5 pb-1 md:mx-0 md:flex-wrap md:px-0"
      >
        <FilterChip label="All" pressed={selected === null} onClick={() => choose(null)} />
        {groups.map((group) => (
          <FilterChip
            key={group.slug}
            label={group.title}
            pressed={selected === group.slug}
            onClick={() => choose(group.slug)}
          />
        ))}
      </div>
      <div className="mt-6 flex flex-col gap-6">
        {shown.map((group) => (
          <section key={group.slug} aria-label={group.title}>
            <h2 className="mb-2 flex items-baseline justify-between font-sans text-[15px] font-semibold">
              {group.title}
              <span className="font-mono text-[12px] font-normal text-muted tabular-nums">
                {plural(group.sheets.length, 'cheat sheet')}
              </span>
            </h2>
            <ul className="overflow-hidden rounded-lg border border-rule bg-surface">
              {group.sheets.map((sheet) => (
                <li key={sheet.lessonId} className="border-t border-rule first:border-t-0">
                  <SheetRow sheet={sheet} />
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    </>
  );
}

function FilterChip({ label, pressed, onClick }: { label: string; pressed: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      aria-pressed={pressed}
      onClick={onClick}
      className={cn(
        'h-9 shrink-0 rounded-full border px-3.5 text-[14px] font-medium whitespace-nowrap select-none',
        'transition-[transform,background-color,border-color,color] duration-[120ms] ease-out active:scale-[0.97]',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        pressed ? 'border-accent bg-accent text-white' : 'border-rule bg-surface text-fg hover:bg-fg/[0.04]',
      )}
    >
      {label}
    </button>
  );
}

function SheetRow({ sheet }: { sheet: CheatSheetSummary }) {
  const preload = usePreload(content.cheatSheet(sheet.lessonId));
  return (
    <Link
      to={`/revise/${sheet.moduleSlug}/${sheet.lessonSlug}`}
      {...preload}
      className={cn(
        'flex items-center gap-3 px-4 py-3.5 select-none',
        'transition-colors duration-150 ease-out active:bg-fg/[0.05] hover:bg-fg/[0.025]',
        'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-accent',
      )}
    >
      <span className="shrink-0 text-accent">
        <Icon d={boltIcon} size={18} />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block leading-snug font-semibold wrap-anywhere">{sheet.title}</span>
        <span className="mt-0.5 block truncate text-[14px] text-muted" title={sheet.lessonTitle}>
          from {sheet.lessonTitle}
        </span>
      </span>
      <span className="shrink-0 text-muted">
        <Icon d={chevronRight} size={18} />
      </span>
    </Link>
  );
}
