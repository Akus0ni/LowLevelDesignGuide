import { Link } from 'react-router';
import type { SearchHit } from '../../data/types';
import { Icon } from '../../ui/Icon';
import { parseTags } from '../notes/tags';
import { useNotes } from '../notes/useNotes';
import { hitReference, KIND_ICON, KIND_LABEL } from './searchKinds';
import { hitLink, snippetParts } from './searchResults';

/** The matched words as highlighter marks: `<mark>`, which screen readers announce as highlighted, so colour is never the only signal. */
export function SnippetText({ hit }: { hit: SearchHit }) {
  return (
    <span className="search-snippet">
      {snippetParts(hit.snippet).map((part, i) =>
        part.marked ? (
          <mark key={i} data-search="">
            {part.text}
          </mark>
        ) : (
          <span key={i}>{part.text}</span>
        ),
      )}
    </span>
  );
}

interface RowProps {
  hit: SearchHit;
  query: string;
  /** Called as the result opens, before navigation (saves the recent search). */
  onOpen: () => void;
  /** The Best match: the lesson title large, above the reference. */
  best?: boolean;
}

/** One search result (spec §3.4): kind glyph, reference, a two-line snippet; notes carry «my note» and their tags. */
export function ResultRow({ hit, query, onOpen, best = false }: RowProps) {
  return (
    <Link
      to={hitLink(hit, query)}
      onClick={onOpen}
      className={best ? 'search-hit search-best' : 'search-hit'}
      data-search-hit=""
      data-kind={hit.kind}
    >
      <span className="search-hit-kind" aria-hidden="true">
        <Icon d={KIND_ICON[hit.kind]} size={best ? 20 : 16} />
      </span>
      <span className="min-w-0 flex-1">
        {best && <span className="search-best-label">Best match</span>}
        {best && <span className="search-best-title">{hit.lessonTitle || 'Removed lesson'}</span>}
        <span className="search-hit-ref">
          <span className="sr-only">{KIND_LABEL[hit.kind]}: </span>
          {hit.kind === 'note' && <span className="search-hit-stereotype">«my note» </span>}
          {hit.kind === 'cheatSheet' && <span className="search-hit-stereotype">«cheat sheet» </span>}
          {best && !hit.heading ? KIND_LABEL[hit.kind] : hitReference(hit)}
        </span>
        {hit.snippet.text && <SnippetText hit={hit} />}
        {hit.kind === 'note' && hit.noteId && <NoteTags noteId={hit.noteId} />}
      </span>
    </Link>
  );
}

function NoteTags({ noteId }: { noteId: string }) {
  const note = useNotes().byId.get(noteId);
  const tags = note ? parseTags(note.body) : [];
  if (tags.length === 0) return null;
  return (
    <span className="search-hit-tags">
      {tags.map((tag) => (
        <span key={tag} className="note-tag">
          #{tag}
        </span>
      ))}
    </span>
  );
}
