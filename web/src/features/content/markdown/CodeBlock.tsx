import { useEffect, useState } from 'react';
import type { ThemedToken } from 'shiki/core';
import { highlight, languageOf } from './highlighter';

/**
 * A fenced code block. Plain text renders at once; highlighting replaces it in the same box when ready (same font,
 * same line height), so nothing moves. The block scrolls sideways on its own; the page never does.
 */
export function CodeBlock({ code, info }: { code: string; info?: string }) {
  const language = languageOf(info);
  const [lines, setLines] = useState<ThemedToken[][] | null>(null);

  useEffect(() => {
    if (!language) return;
    let current = true;
    highlight(code, language).then(
      (tokens) => current && setLines(tokens),
      () => {}, // Plain text is a fine fallback.
    );
    return () => {
      current = false;
    };
  }, [code, language]);

  return (
    <figure className="code-block">
      {info && <figcaption className="code-block-label">{info}</figcaption>}
      <pre tabIndex={0} aria-label={info ? `${info} code` : 'Code'}>
        <code>
          {lines
            ? lines.map((line, index) => (
                <span key={index} className="code-line">
                  {line.map((token, t) => (
                    <span key={t} style={token.htmlStyle as React.CSSProperties}>
                      {token.content}
                    </span>
                  ))}
                  {'\n'}
                </span>
              ))
            : code}
        </code>
      </pre>
    </figure>
  );
}
