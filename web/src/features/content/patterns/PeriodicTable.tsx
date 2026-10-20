import { useEffect, useRef, useState, type CSSProperties, type KeyboardEvent } from 'react';
import { Link } from 'react-router';
import type { LessonSummary, Lookalike, ModuleMap } from '../../../data/types';
import { statusOf } from '../../progress/progressMath';
import { StatusMark } from '../../progress/StatusMark';
import { useProgress } from '../../progress/useProgress';
import { LessonIcon } from '../LessonIcon';
import { relationsOf, type Relation } from '../mapRelations';
import { formatMinutes } from '../navigation';
import { useClickOutside } from '../useClickOutside';
import { comparePath, lookalikesOf, partnersOf } from './lookalikes';

/** Category tones (`--category-1..4` in index.css); a group's stereotype picks its tone, others take the next free one. */
const TONES: Record<string, number> = { creational: 1, structural: 2, behavioral: 3, enterprise: 4 };

interface Tile {
  lesson: LessonSummary;
  position: number;
}

/**
 * A pattern module's lessons as a periodic table (spec 006 §3.5): one coloured block per category (the map's groups),
 * one tile per pattern with its two-letter symbol. Tapping a tile keeps it, the patterns it travels with (solid ring)
 * and its look-alikes (dashed ring), steps the rest back and opens a card. Arrow keys move between tiles; Esc or a tap
 * outside clears.
 */
export function PeriodicTable({
  map,
  lessons,
  lookalikes,
}: {
  map: ModuleMap;
  lessons: LessonSummary[];
  lookalikes?: Lookalike[] | null;
}) {
  const [selected, setSelected] = useState<string | null>(null);
  const [active, setActive] = useState<string | null>(null);
  const root = useRef<HTMLElement>(null);
  const tiles = useRef(new Map<string, HTMLButtonElement>());
  const walked = useRef(false);
  const { byId } = useProgress();

  const bySlug = new Map<string, Tile>(lessons.map((lesson, index) => [lesson.slug, { lesson, position: index + 1 }]));
  const order = map.groups.flatMap((group) => group.lessons).filter((slug) => bySlug.has(slug));
  const relations = selected ? relationsOf(map, selected) : [];
  const travels = new Set(relations.map((relation) => relation.slug));
  const partners = new Set(selected ? partnersOf(lookalikes, selected) : []);
  const focusable = active ?? selected ?? order[0];

  useClickOutside(root, selected !== null, () => setSelected(null));

  useEffect(() => {
    if (!walked.current || !selected) return;
    walked.current = false;
    tiles.current.get(selected)?.focus({ preventScroll: true });
  }, [selected]);

  const move = (from: string, key: string): string | undefined => {
    const index = order.indexOf(from);
    if (key === 'ArrowRight') return order[index + 1];
    if (key === 'ArrowLeft') return order[index - 1];
    if (key === 'Home') return order[0];
    if (key === 'End') return order[order.length - 1];
    if (key === 'ArrowDown' || key === 'ArrowUp') return nearestInNextRow(from, key === 'ArrowDown' ? 1 : -1);
    return undefined;
  };

  /** The tile in the row below (or above) whose centre is closest, whatever the grid's current column count. */
  const nearestInNextRow = (from: string, direction: 1 | -1): string | undefined => {
    const origin = tiles.current.get(from)?.getBoundingClientRect();
    if (!origin) return undefined;
    const candidates = order
      .map((slug) => ({ slug, box: tiles.current.get(slug)?.getBoundingClientRect() }))
      .filter((c): c is { slug: string; box: DOMRect } => !!c.box && (c.box.top - origin.top) * direction > origin.height / 2);
    if (candidates.length === 0) return undefined;
    const rowTop = direction === 1 ? Math.min(...candidates.map((c) => c.box.top)) : Math.max(...candidates.map((c) => c.box.top));
    const centre = origin.left + origin.width / 2;
    return candidates
      .filter((c) => Math.abs(c.box.top - rowTop) < origin.height / 2)
      .sort((a, b) => Math.abs(a.box.left + a.box.width / 2 - centre) - Math.abs(b.box.left + b.box.width / 2 - centre))[0]?.slug;
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Escape' && selected) {
      tiles.current.get(selected)?.focus();
      setSelected(null);
      return;
    }
    const from = (event.target as HTMLElement).dataset.slug;
    const to = from ? move(from, event.key) : undefined;
    if (!to) return;
    event.preventDefault();
    setActive(to);
    tiles.current.get(to)?.focus();
  };

  const walk = (slug: string) => {
    walked.current = true;
    setActive(slug);
    setSelected(slug);
  };

  return (
    <section
      ref={root}
      aria-label="Periodic table of patterns"
      className="ptable"
      data-focus={selected ? '' : undefined}
      onKeyDown={onKeyDown}
    >
      <p className="map-head">
        <span>periodic table</span>
        <span>{selected ? 'solid: travels with · dashed: look-alike' : 'tap a pattern'}</span>
      </p>
      {map.groups.map((group, groupIndex) => {
        const [stereotype, subtitle] = splitTitle(group.title);
        const tone = TONES[stereotype.toLowerCase()] ?? (groupIndex % 4) + 1;
        const card = selected && group.lessons.includes(selected) ? bySlug.get(selected) : undefined;
        return (
          <div key={group.title || '(more)'} className="ptable-block" style={{ '--category': `var(--category-${tone})` } as CSSProperties}>
            <p className="ptable-block-head">
              <span className="ptable-stereotype">«{stereotype.toLowerCase()}»</span>
              {subtitle && <span className="ptable-subtitle">{subtitle}</span>}
            </p>
            <ul className="ptable-grid">
              {group.lessons.map((slug) => {
                const tile = bySlug.get(slug);
                if (!tile) return null;
                const status = statusOf(byId, tile.lesson.id);
                const state =
                  slug === selected ? 'selected' : travels.has(slug) ? 'related' : partners.has(slug) ? 'lookalike' : undefined;
                return (
                  <li key={slug}>
                    <button
                      ref={(element) => {
                        if (element) tiles.current.set(slug, element);
                        else tiles.current.delete(slug);
                      }}
                      type="button"
                      className="ptable-tile"
                      data-slug={slug}
                      data-state={state}
                      aria-pressed={slug === selected}
                      aria-label={`${tile.lesson.title} (${tile.lesson.symbol})`}
                      tabIndex={slug === focusable ? 0 : -1}
                      onFocus={() => setActive(slug)}
                      onClick={() => setSelected((current) => (current === slug ? null : slug))}
                    >
                      <span aria-hidden="true" className="ptable-number">
                        {String(tile.position).padStart(2, '0')}
                      </span>
                      <LessonIcon icon={tile.lesson.icon} className="ptable-icon" />
                      <span aria-hidden="true" className="ptable-symbol">
                        {tile.lesson.symbol}
                      </span>
                      <span aria-hidden="true" className="ptable-name">
                        {tile.lesson.title}
                      </span>
                      {status !== 'notStarted' && <StatusMark status={status} className="ptable-status" />}
                    </button>
                  </li>
                );
              })}
            </ul>
            {card && (
              <div className="map-card ptable-card" role="region" aria-label={card.lesson.title}>
                <TileCard
                  tile={card}
                  relations={relations}
                  lookalikes={lookalikesOf(lookalikes, card.lesson.slug)}
                  titleOf={(slug) => bySlug.get(slug)?.lesson.title ?? slug}
                  onWalk={walk}
                />
              </div>
            )}
          </div>
        );
      })}
      <p className="sr-only" aria-live="polite">
        {selected ? describe(selected, relations, [...partners], (slug) => bySlug.get(slug)?.lesson.title ?? slug) : ''}
      </p>
    </section>
  );
}

function TileCard({
  tile,
  relations,
  lookalikes,
  titleOf,
  onWalk,
}: {
  tile: Tile;
  relations: Relation[];
  lookalikes: Lookalike[];
  titleOf: (slug: string) => string;
  onWalk: (slug: string) => void;
}) {
  const { lesson, position } = tile;
  return (
    <>
      <p className="map-card-meta tabular-nums">
        {lesson.symbol} · pattern {String(position).padStart(2, '0')} · {formatMinutes(lesson.readingMinutes)}
      </p>
      <p className="map-card-title">{lesson.title}</p>
      {lesson.hook && <p className="ptable-hook">“{lesson.hook}”</p>}
      <p className="map-card-summary">{lesson.summary}</p>
      {relations.length > 0 && (
        <ul className="map-relations" aria-label="Travels with">
          {relations.map((relation) => (
            <li key={`${relation.direction}-${relation.slug}`}>
              <button type="button" className="map-relation" onClick={() => onWalk(relation.slug)}>
                {relation.direction === 'out' ? (
                  <>
                    <span className="text-muted">{relation.label}</span> <strong>{titleOf(relation.slug)}</strong>
                  </>
                ) : (
                  <>
                    <strong>{titleOf(relation.slug)}</strong> <span className="text-muted">{relation.label} this</span>
                  </>
                )}
              </button>
            </li>
          ))}
        </ul>
      )}
      {lookalikes.length > 0 && (
        <ul className="ptable-lookalikes" aria-label="Often confused with">
          {lookalikes.map((lookalike) => (
            <li key={lookalike.lessons.join()}>
              <Link to={comparePath(lesson.moduleSlug, lookalike)} className="ptable-compare">
                Compare {lookalike.lessons.map(titleOf).join(' · ')} →
              </Link>
            </li>
          ))}
        </ul>
      )}
      <Link to={`/learn/${lesson.moduleSlug}/${lesson.slug}`} className="map-open">
        Open lesson →
      </Link>
    </>
  );
}

/** "Creational: making objects" → ["Creational", "Making objects"]; a title without a colon is its own stereotype. */
function splitTitle(title: string): [string, string] {
  const colon = title.indexOf(':');
  if (colon < 0) return [title || 'more', ''];
  const rest = title.slice(colon + 1).trim();
  return [title.slice(0, colon).trim(), rest.charAt(0).toUpperCase() + rest.slice(1)];
}

function describe(slug: string, relations: Relation[], partners: string[], titleOf: (slug: string) => string): string {
  const travels = relations.map((r) => titleOf(r.slug));
  return [
    titleOf(slug),
    travels.length > 0 ? `travels with ${travels.join(', ')}` : '',
    partners.length > 0 ? `often confused with ${partners.map(titleOf).join(', ')}` : '',
  ]
    .filter(Boolean)
    .join('; ');
}
