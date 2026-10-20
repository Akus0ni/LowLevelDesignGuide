import { useRef, useState, type KeyboardEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import type { SearchKind, SearchResults } from '../../data/types';
import { useModules } from '../../data/useContent';
import { Icon } from '../../ui/Icon';
import { ContentBoundary } from '../content/ContentBoundary';
import { ListSkeleton } from '../content/Skeletons';
import { addRecent, clearRecent, loadRecent, type RecentSearch } from './recentSearches';
import { ResultRow } from './ResultRow';
import { KIND_LABEL } from './searchKinds';
import { readSearchState, writeSearchState, type SearchState } from './searchParams';
import { groupResults, hitLink } from './searchResults';
import { isSearchable, useSearch } from './useSearch';
import { useSearchOwner } from './useSearchOwner';

const searchIcon = ['M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z', 'm20 20-3.5-3.5'];
const clockIcon = ['M12 7v5l3 2', 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Z'];
const KIND_FILTERS: { kind: SearchKind | null; label: string }[] = [
  { kind: null, label: 'All' },
  { kind: 'lesson', label: 'Lessons' },
  { kind: 'cheatSheet', label: 'Cheat sheets' },
  { kind: 'note', label: 'Notes' },
];
const REMOVED_LESSONS = 'Removed lessons';

/** The Search tab (spec 009 §3.4): one box, filters in the URL, results grouped by module with a Best match on top. */
export function SearchPage() {
  const [params, setParams] = useSearchParams();
  const state = readSearchState(params);
  const update = (next: Partial<SearchState>) =>
    setParams(writeSearchState({ ...state, ...next }), { replace: true });
  const input = useRef<HTMLInputElement>(null);
  const list = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const search = useSearch(state.query, { kind: state.kind, module: state.module });
  const owner = useSearchOwner();
  const [recent, setRecent] = useState(() => loadRecent(owner));

  const remember = () =>
    setRecent(addRecent(owner, { query: state.query, kind: state.kind, module: state.module }));
  const openTop = () => {
    const top = search.answered === state.query ? search.results?.hits[0] : undefined;
    if (!top) return;
    remember();
    void navigate(hitLink(top, state.query));
  };
  const onBoxKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault();
      openTop();
    } else if (event.key === 'Escape' && state.query) {
      event.preventDefault();
      update({ query: '' });
    } else if (event.key === 'ArrowDown') {
      const first = list.current?.querySelector<HTMLElement>('[data-search-hit]');
      if (first) {
        event.preventDefault();
        first.focus();
      }
    }
  };
  const onListKey = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    const hits = [...(list.current?.querySelectorAll<HTMLElement>('[data-search-hit]') ?? [])];
    const at = hits.indexOf(document.activeElement as HTMLElement);
    if (at < 0) return;
    event.preventDefault();
    const next = event.key === 'ArrowDown' ? hits[at + 1] : at === 0 ? input.current : hits[at - 1];
    next?.focus();
  };

  return (
    <section className="pt-8 md:pt-12">
      <title>Search · LLD Guide</title>
      <h1 className="text-[34px] font-semibold md:text-5xl">Search</h1>
      <form role="search" className="search-box" onSubmit={(event) => event.preventDefault()}>
        <span className="search-box-icon" aria-hidden="true">
          <Icon d={searchIcon} size={20} />
        </span>
        <input
          ref={input}
          type="search"
          value={state.query}
          onChange={(event) => update({ query: event.target.value })}
          onKeyDown={onBoxKey}
          placeholder="Patterns, principles, diagrams, your notes"
          aria-label="Search lessons, cheat sheets and your notes"
          enterKeyHint="search"
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          maxLength={100}
          autoFocus
          className="search-box-input"
        />
        {state.query && (
          <button
            type="button"
            className="search-box-clear"
            onClick={() => {
              update({ query: '' });
              input.current?.focus();
            }}
          >
            Clear
          </button>
        )}
      </form>
      <ContentBoundary fallback={<ListSkeleton />}>
        <Filters state={state} counts={search.results?.countsByKind} onChange={update} />
      </ContentBoundary>
      <div ref={list} onKeyDown={onListKey}>
        {isSearchable(state.query) ? (
          <ContentBoundary fallback={<ListSkeleton />}>
            <Results
              state={state}
              results={search.results}
              error={search.error}
              onOpen={remember}
              onClearFilters={() => update({ kind: null, module: null })}
            />
          </ContentBoundary>
        ) : (
          <EmptyBox
            recent={recent}
            suggestions={search.results?.suggestions ?? []}
            onPick={(pick) => update(pick)}
            onClear={() => {
              clearRecent(owner);
              setRecent([]);
            }}
          />
        )}
      </div>
    </section>
  );
}

function Filters({
  state,
  counts,
  onChange,
}: {
  state: SearchState;
  counts: SearchResults['countsByKind'] | undefined;
  onChange: (next: Partial<SearchState>) => void;
}) {
  const modules = useModules();
  const show = isSearchable(state.query) && counts;
  const count = (kind: SearchKind | null) =>
    counts ? (kind ? counts[kind] : counts.lesson + counts.cheatSheet + counts.note) : 0;
  return (
    <div className="search-filters">
      <div className="search-kinds" role="group" aria-label="Show">
        {KIND_FILTERS.map(({ kind, label }) => (
          <button
            key={label}
            type="button"
            aria-pressed={state.kind === kind}
            onClick={() => onChange({ kind })}
            className="search-kind"
          >
            {label}
            {show && <span className="search-kind-count">{count(kind)}</span>}
          </button>
        ))}
      </div>
      <select
        value={state.module ?? ''}
        onChange={(event) => onChange({ module: event.target.value || null })}
        aria-label="Module"
        className="search-module"
      >
        <option value="">All modules</option>
        {modules.map((module) => (
          <option key={module.slug} value={module.slug}>
            {module.title}
          </option>
        ))}
      </select>
    </div>
  );
}

function Results({
  state,
  results,
  error,
  onOpen,
  onClearFilters,
}: {
  state: SearchState;
  results: SearchResults | null;
  error: boolean;
  onOpen: () => void;
  onClearFilters: () => void;
}) {
  const modules = useModules();
  if (error && !results)
    return <p className="search-empty">Search isn't available right now. Restart the app and try again.</p>;
  if (!results) return null;
  const title = (slug: string) =>
    slug === '' ? REMOVED_LESSONS : (modules.find((m) => m.slug === slug)?.title ?? slug);
  const { best, groups } = groupResults(results.hits);
  const filtered = state.kind !== null || state.module !== null;

  return (
    <>
      <p className="search-status" aria-live="polite">
        {results.total === 0 ? (
          <>
            No matches for <strong>{results.query.trim()}</strong>.
          </>
        ) : (
          <>
            <span className="tabular-nums">{results.total}</span> result{results.total === 1 ? '' : 's'}
            {results.total > results.hits.length && ` · showing the first ${results.hits.length}`}
          </>
        )}
        {results.expansions.length > 0 && (
          <span className="search-also"> Also searching: {results.expansions.join(', ')}.</span>
        )}
      </p>
      {results.total === 0 && filtered && (
        <button type="button" className="search-clear-filters" onClick={onClearFilters}>
          Search everything instead
        </button>
      )}
      {best && <ResultRow hit={best} query={state.query} onOpen={onOpen} best />}
      {groups.map((group) => (
        <section key={group.moduleSlug} className="search-group" aria-label={title(group.moduleSlug)}>
          <h2 className="search-group-title">
            {title(group.moduleSlug)} <span className="search-group-count">{group.hits.length}</span>
          </h2>
          <ul className="search-list">
            {group.hits.map((hit) => (
              <li key={`${hit.kind}|${hit.lessonId}|${hit.anchor}|${hit.noteId ?? ''}`}>
                <ResultRow hit={hit} query={state.query} onOpen={onOpen} />
              </li>
            ))}
          </ul>
        </section>
      ))}
    </>
  );
}

function EmptyBox({
  recent,
  suggestions,
  onPick,
  onClear,
}: {
  recent: RecentSearch[];
  suggestions: string[];
  onPick: (pick: Partial<SearchState>) => void;
  onClear: () => void;
}) {
  return (
    <div className="search-start">
      {recent.length > 0 && (
        <section aria-labelledby="search-recent-title">
          <div className="search-start-head">
            <h2 id="search-recent-title" className="search-start-title">
              Recent
            </h2>
            <button type="button" className="search-start-clear" onClick={onClear}>
              Clear
            </button>
          </div>
          <ul className="search-recent">
            {recent.map((item) => (
              <li key={item.query}>
                <button type="button" className="search-recent-item" onClick={() => onPick(item)}>
                  <Icon d={clockIcon} size={16} />
                  <span className="min-w-0 flex-1 truncate">{item.query}</span>
                  {item.kind && <span className="search-recent-kind">{KIND_LABEL[item.kind]}</span>}
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}
      {suggestions.length > 0 && (
        <section aria-labelledby="search-suggestions-title">
          <h2 id="search-suggestions-title" className="search-start-title">
            Try
          </h2>
          <div className="search-suggestions">
            {suggestions.map((suggestion) => (
              <button
                key={suggestion}
                type="button"
                className="search-suggestion"
                onClick={() => onPick({ query: suggestion })}
              >
                {suggestion}
              </button>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
