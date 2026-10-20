import { Fragment, use, useEffect, useState } from 'react';
import type { ThemedToken } from 'shiki/core';
import type { LessonTerm } from '../../../data/types';
import type { MarkKind } from './fence';
import { highlight, languageOf } from './highlighter';
import { TermCallout } from './TermCallout';
import { splitByTerms, TermsContext } from './terms';

const NO_MARKS: ReadonlySet<number> = new Set();

/**
 * A fenced code block: a top compartment with the language (and the fence's title), then the code. Plain text renders
 * at once; highlighting replaces it in the same box when ready (same font, same line height), so nothing moves. The
 * block scrolls sideways on its own; the page never does.
 */
export function CodeBlock({
  code,
  info,
  title,
  marks = NO_MARKS,
  markKind = 'mark',
}: {
  code: string;
  info?: string;
  title?: string;
  marks?: ReadonlySet<number>;
  markKind?: MarkKind;
}) {
  return (
    <figure className="code-block">
      {(info || title) && (
        <figcaption className="code-block-label">
          {title && <span className="code-block-title">{title}</span>}
          {info && <span>{info}</span>}
        </figcaption>
      )}
      <CodeLines code={code} info={info} marks={marks} markKind={markKind} label={title ?? info} />
    </figure>
  );
}

/**
 * The `<pre>` itself, shared by code blocks and both panes of a Before / After compare. Each line is its own span (a
 * marked line is a full-width block); the line breaks sit between the spans so copied code keeps them.
 */
export function CodeLines({
  code,
  info,
  marks = NO_MARKS,
  markKind = 'mark',
  label,
  linkable = NO_MARKS,
}: {
  code: string;
  info?: string;
  marks?: ReadonlySet<number>;
  markKind?: MarkKind;
  label?: string;
  /** Lines that stand for a part of a paired diagram (spec 008 §3.4): they take taps. */
  linkable?: ReadonlySet<number>;
}) {
  const language = languageOf(info);
  const terms = use(TermsContext);
  const [tokens, setTokens] = useState<ThemedToken[][] | null>(null);

  useEffect(() => {
    if (!language) return;
    let current = true;
    highlight(code, language).then(
      (lines) => current && setTokens(lines),
      () => {}, // Plain text is a fine fallback.
    );
    return () => {
      current = false;
    };
  }, [code, language]);

  const lines: (ThemedToken[] | string)[] = tokens ?? code.split('\n');

  return (
    <pre tabIndex={0} aria-label={label ? `${label} code` : 'Code'} data-marked={marks.size > 0 || undefined}>
      <code>
        {lines.map((line, index) => (
          <Fragment key={index}>
            <span
              className="code-line"
              data-mark={marks.has(index + 1) ? markKind : undefined}
              data-linkable={linkable.has(index + 1) || undefined}
            >
              {typeof line === 'string' ? (
                <WithTerms text={line} terms={terms} />
              ) : (
                line.map((token, t) => (
                  <span key={t} style={token.htmlStyle as React.CSSProperties}>
                    <WithTerms text={token.content} terms={terms} />
                  </span>
                ))
              )}
            </span>
            {index < lines.length - 1 && '\n'}
          </Fragment>
        ))}
      </code>
    </pre>
  );
}

/** Text with the lesson's terms turned into callouts. */
function WithTerms({ text, terms }: { text: string; terms: LessonTerm[] }) {
  return splitByTerms(text, terms).map((part, index) =>
    part.term ? (
      <TermCallout key={index} term={part.term}>
        {part.text}
      </TermCallout>
    ) : (
      part.text
    ),
  );
}
