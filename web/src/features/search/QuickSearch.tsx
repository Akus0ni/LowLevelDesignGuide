import { Dialog } from '@base-ui/react/dialog';
import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useNavigate } from 'react-router';
import type { SearchHit } from '../../data/types';
import { Icon } from '../../ui/Icon';
import { useMediaQuery } from '../../ui/useMediaQuery';
import { addRecent } from './recentSearches';
import { hitReference, KIND_ICON, KIND_LABEL } from './searchKinds';
import { hitLink } from './searchResults';
import { SnippetText } from './ResultRow';
import { isSearchable, useSearch } from './useSearch';
import { useSearchOwner } from './useSearchOwner';

/** A precise pointer and room for a dialog: desktops and Windows, not phones (spec §3.7). */
const QUICK_SEARCH_MEDIA = '(min-width: 48rem) and (pointer: fine)';

/** At most this many hits of each kind; the Search tab has the rest. */
const PER_KIND = 8;

const searchIcon = ['M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z', 'm20 20-3.5-3.5'];

/** `/` opens search only when the learner isn't typing somewhere already. */
function isTyping(target: EventTarget | null): boolean {
  const element = target as HTMLElement | null;
  return !!element?.closest?.('input, textarea, select, [contenteditable=""], [contenteditable="true"]');
}

function firstOfEachKind(hits: readonly SearchHit[]): SearchHit[] {
  const seen = { lesson: 0, cheatSheet: 0, note: 0 };
  return hits.filter((hit) => seen[hit.kind]++ < PER_KIND);
}

/**
 * Quick search (spec 009 §3.7): `Ctrl+K` / `⌘K` or `/` from any screen opens a dialog with the same results as the
 * Search tab. It is used many times a day from the keyboard, so it opens and closes without animation
 * (emil-design-eng); Escape closes it and focus goes back where it was.
 */
export function QuickSearch() {
  const enabled = useMediaQuery(QUICK_SEARCH_MEDIA);
  const [open, setOpen] = useState(false);
  const returnTo = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!enabled) return;
    const onKey = (event: globalThis.KeyboardEvent) => {
      const combo = (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k';
      const slash =
        event.key === '/' && !event.ctrlKey && !event.metaKey && !event.altKey && !isTyping(event.target);
      if (!combo && !slash) return;
      event.preventDefault();
      returnTo.current = document.activeElement as HTMLElement | null;
      setOpen(true);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [enabled]);

  if (!enabled) return null;
  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Portal>
        <Dialog.Backdrop className="quick-search-backdrop" />
        <Dialog.Popup className="quick-search" finalFocus={returnTo}>
          <Dialog.Title className="sr-only">Search</Dialog.Title>
          {open && <QuickSearchBody onDone={() => setOpen(false)} />}
        </Dialog.Popup>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

function QuickSearchBody({ onDone }: { onDone: () => void }) {
  const [query, setQuery] = useState('');
  const [active, setActive] = useState(0);
  const navigate = useNavigate();
  const owner = useSearchOwner();
  const search = useSearch(query);
  const searchable = isSearchable(query);
  const hits = searchable && search.answered === query ? firstOfEachKind(search.results?.hits ?? []) : [];
  const seeAll = `/search?${new URLSearchParams({ q: query.trim() })}`;
  // The last option is always "See all results", so Enter does something even with no hits.
  const options = [...hits.map((hit) => hitLink(hit, query)), ...(searchable ? [seeAll] : [])];

  const go = (to: string, hit?: SearchHit) => {
    if (hit) addRecent(owner, { query, kind: null, module: null });
    onDone();
    void navigate(to);
  };
  const onKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (options.length === 0) return;
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const step = event.key === 'ArrowDown' ? 1 : -1;
      setActive((index) => (index + step + options.length) % options.length);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const index = Math.min(active, options.length - 1);
      go(options[index], hits[index]);
    }
  };
  const optionId = (index: number) => `quick-search-option-${index}`;

  return (
    <>
      <div className="quick-search-box">
        <Icon d={searchIcon} size={20} />
        <input
          type="search"
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={onKey}
          placeholder="Search lessons, cheat sheets and your notes"
          aria-label="Search"
          role="combobox"
          aria-expanded={options.length > 0}
          aria-controls="quick-search-results"
          aria-activedescendant={
            options.length > 0 ? optionId(Math.min(active, options.length - 1)) : undefined
          }
          autoComplete="off"
          spellCheck={false}
          maxLength={100}
          className="quick-search-input"
        />
        <kbd className="quick-search-key">Esc</kbd>
      </div>
      <ul id="quick-search-results" role="listbox" aria-label="Results" className="quick-search-results">
        {hits.map((hit, index) => (
          <li
            key={`${hit.kind}|${hit.lessonId}|${hit.anchor}|${hit.noteId ?? ''}`}
            id={optionId(index)}
            role="option"
            aria-selected={index === active}
            className="quick-search-option"
            data-kind={hit.kind}
            onPointerMove={() => setActive(index)}
            onClick={() => go(options[index], hit)}
          >
            <span className="search-hit-kind" aria-hidden="true">
              <Icon d={KIND_ICON[hit.kind]} size={16} />
            </span>
            <span className="min-w-0 flex-1">
              <span className="search-hit-ref">
                <span className="sr-only">{KIND_LABEL[hit.kind]}: </span>
                {hitReference(hit)}
              </span>
              {hit.snippet.text && <SnippetText hit={hit} />}
            </span>
          </li>
        ))}
        {searchable && (
          <li
            id={optionId(hits.length)}
            role="option"
            aria-selected={active === hits.length}
            className="quick-search-option quick-search-all"
            onPointerMove={() => setActive(hits.length)}
            onClick={() => go(seeAll)}
          >
            {search.answered === query && hits.length === 0
              ? `No matches. Open Search for “${query.trim()}”`
              : 'See all results'}
          </li>
        )}
      </ul>
    </>
  );
}
