import { useEffect, useId, useState, useSyncExternalStore } from 'react';
import { CodeBlock } from './CodeBlock';
import { DiagramViewer } from './DiagramViewer';
import { InteractiveDiagram } from './InteractiveDiagram';
import { currentColorScheme, renderMermaid } from './mermaid';

const DARK = '(prefers-color-scheme: dark)';

function subscribeToScheme(onChange: () => void): () => void {
  const query = window.matchMedia(DARK);
  query.addEventListener('change', onChange);
  return () => query.removeEventListener('change', onChange);
}

type Diagram =
  { status: 'loading' } | { status: 'ready'; svg: string } | { status: 'failed'; message: string };

/**
 * A ```mermaid block. Mermaid loads only when a lesson has a diagram, and the diagram re-renders when the system
 * switches between light and dark. If the source has an error, the reader still sees it, with a short note.
 */
export function MermaidDiagram({ source }: { source: string }) {
  const scheme = useSyncExternalStore(subscribeToScheme, currentColorScheme, () => 'light');
  const id = `mermaid-${useId().replace(/[^a-zA-Z0-9]/g, '')}`;
  const [diagram, setDiagram] = useState<Diagram>({ status: 'loading' });

  useEffect(() => {
    let current = true;
    renderMermaid(`${id}-${scheme}`, source).then(
      (svg) => current && setDiagram({ status: 'ready', svg }),
      (error: unknown) =>
        current &&
        setDiagram({ status: 'failed', message: error instanceof Error ? error.message : String(error) }),
    );
    return () => {
      current = false;
    };
  }, [id, scheme, source]);

  if (diagram.status === 'failed') {
    return (
      <figure className="diagram-failed">
        <figcaption>This diagram couldn't be drawn, so here is its source.</figcaption>
        <CodeBlock code={source} info="mermaid" />
      </figure>
    );
  }

  if (diagram.status === 'loading') {
    return (
      <div className="diagram diagram-loading" aria-busy="true">
        <span className="sr-only">Drawing diagram…</span>
      </div>
    );
  }

  const label = diagramKind(source);
  return (
    <InteractiveDiagram
      svg={diagram.svg}
      label={label}
      className="diagram"
      canvasClassName="diagram-canvas"
      actions={<DiagramViewer svg={diagram.svg} label={label} />}
    />
  );
}

const KINDS: Record<string, string> = {
  classDiagram: 'class diagram',
  sequenceDiagram: 'sequence diagram',
  'stateDiagram-v2': 'state diagram',
  stateDiagram: 'state diagram',
  flowchart: 'flowchart',
  graph: 'flowchart',
  erDiagram: 'entity diagram',
};

/** "class diagram", "sequence diagram"…: names the figure for screen readers and the viewer's title. */
function diagramKind(source: string): string {
  const first = source.trim().split(/\s+/)[0] ?? '';
  return KINDS[first] ?? 'diagram';
}
