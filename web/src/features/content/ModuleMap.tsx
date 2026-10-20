import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useClickOutside } from './useClickOutside';
import { Link } from 'react-router';
import type { LessonSummary, ModuleMap as Map } from '../../data/types';
import { relationsOf, type Relation } from './mapRelations';
import { LessonIcon } from './LessonIcon';
import { formatMinutes } from './navigation';

/**
 * How a module's lessons relate (spec 004 §3.6), drawn in the app's class-diagram identity: each group is a
 * «package» box and its lessons are members. Tapping a lesson keeps it and the lessons it links to, steps the rest back,
 * and opens a card under its box; the card's links walk the map. Tap again, tap outside or press Esc to clear.
 */
export function ModuleMap({ map, lessons }: { map: Map; lessons: LessonSummary[] }) {
  const [selected, setSelected] = useState<string | null>(null);
  const walked = useRef(false);
  const root = useRef<HTMLElement>(null);
  const chips = useRef(new globalThis.Map<string, HTMLButtonElement>());
  const card = useRef<HTMLDivElement>(null);

  const bySlug = new globalThis.Map(lessons.map((lesson, index) => [lesson.slug, { lesson, position: index + 1 }]));
  const relations = selected ? relationsOf(map, selected) : [];
  const direction = new globalThis.Map(relations.map((relation) => [relation.slug, relation.direction]));

  useClickOutside(root, selected !== null, () => setSelected(null));

  // Following a link in the card moves focus (and the card) to the lesson it leads to.
  useEffect(() => {
    if (!walked.current || !selected) return;
    walked.current = false;
    chips.current.get(selected)?.focus({ preventScroll: true });
    card.current?.scrollIntoView({ block: 'nearest' });
  }, [selected]);

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Escape' && selected) {
      chips.current.get(selected)?.focus();
      setSelected(null);
    }
  };

  return (
    <section
      ref={root}
      aria-label="Map of this module"
      className="module-map"
      data-focus={selected ? '' : undefined}
      onKeyDown={onKeyDown}
    >
      <p className="map-head">
        <span>map</span>
        <span>{selected ? `${relations.length} link${relations.length === 1 ? '' : 's'}` : 'tap a lesson to trace it'}</span>
      </p>
      <div className="map-groups">
        {map.groups.map((group) => (
          <div key={group.title || '(more)'} className="map-package">
            <p className="map-stereotype">«package»</p>
            <p className="map-title">{group.title || 'More'}</p>
            <ul className="map-members">
              {group.lessons.map((slug) => {
                const entry = bySlug.get(slug);
                if (!entry) return null;
                const state = slug === selected ? 'selected' : direction.has(slug) ? 'related' : undefined;
                return (
                  <li key={slug}>
                    <button
                      ref={(element) => {
                        if (element) chips.current.set(slug, element);
                        else chips.current.delete(slug);
                      }}
                      type="button"
                      className="map-chip"
                      aria-pressed={slug === selected}
                      data-state={state}
                      data-direction={direction.get(slug)}
                      onClick={() => setSelected((current) => (current === slug ? null : slug))}
                    >
                      {entry.lesson.icon ? (
                        <LessonIcon icon={entry.lesson.icon} />
                      ) : (
                        <span aria-hidden="true" className="map-chip-number">
                          {String(entry.position).padStart(2, '0')}
                        </span>
                      )}
                      {entry.lesson.title}
                    </button>
                  </li>
                );
              })}
            </ul>
            {selected && group.lessons.includes(selected) && bySlug.has(selected) && (
              <div ref={card} className="map-card" role="region" aria-label={bySlug.get(selected)!.lesson.title}>
                <LessonCard
                  entry={bySlug.get(selected)!}
                  relations={relations}
                  titleOf={(slug) => bySlug.get(slug)?.lesson.title ?? slug}
                  onWalk={(slug) => {
                    walked.current = true;
                    setSelected(slug);
                  }}
                />
              </div>
            )}
          </div>
        ))}
      </div>
      <p className="sr-only" aria-live="polite">
        {selected
          ? `${bySlug.get(selected)?.lesson.title}: ${relations.map((r) => describe(r, (s) => bySlug.get(s)?.lesson.title ?? s)).join('; ') || 'no links'}`
          : ''}
      </p>
    </section>
  );
}

function LessonCard({
  entry,
  relations,
  titleOf,
  onWalk,
}: {
  entry: { lesson: LessonSummary; position: number };
  relations: Relation[];
  titleOf: (slug: string) => string;
  onWalk: (slug: string) => void;
}) {
  const { lesson, position } = entry;
  return (
    <>
      <p className="map-card-meta tabular-nums">
        lesson {String(position).padStart(2, '0')} · {formatMinutes(lesson.readingMinutes)}
      </p>
      <p className="map-card-title">{lesson.title}</p>
      <p className="map-card-summary">{lesson.summary}</p>
      {relations.length > 0 && (
        <ul className="map-relations" aria-label="Links">
          {relations.map((relation) => (
            <li key={`${relation.direction}-${relation.slug}`}>
              <button type="button" className="map-relation" onClick={() => onWalk(relation.slug)}>
                {relation.direction === 'out' ? (
                  <>
                    <span className="text-muted">{relation.label}</span> <strong>{titleOf(relation.slug)}</strong>{' '}
                    <span aria-hidden="true">→</span>
                  </>
                ) : (
                  <>
                    <span aria-hidden="true">←</span> <strong>{titleOf(relation.slug)}</strong>{' '}
                    <span className="text-muted">{relation.label} this</span>
                  </>
                )}
              </button>
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

function describe(relation: Relation, titleOf: (slug: string) => string): string {
  return relation.direction === 'out'
    ? `${relation.label} ${titleOf(relation.slug)}`
    : `${titleOf(relation.slug)} ${relation.label} this`;
}
