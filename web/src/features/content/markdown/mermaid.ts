import type { MermaidConfig } from 'mermaid';

export type ColorScheme = 'light' | 'dark';

type Mermaid = typeof import('mermaid').default;

let mermaid: Promise<Mermaid> | null = null;
/** Mermaid's render is not re-entrant: diagrams render one after another. */
let queue: Promise<unknown> = Promise.resolve();

function load(): Promise<Mermaid> {
  mermaid ??= import('mermaid').then((module) => module.default);
  return mermaid;
}

/** Diagrams wear the app's own tokens, so a class diagram looks like the class boxes on Learn. */
function config(): MermaidConfig {
  const token = (name: string) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  const fontFamily = getComputedStyle(document.body).fontFamily;
  // Fit the column (the SVG keeps its natural size as max-width); "Expand" shows it full size.
  const natural = { useMaxWidth: true };
  return {
    startOnLoad: false,
    securityLevel: 'strict',
    theme: 'base',
    fontFamily,
    themeVariables: {
      background: token('--surface'),
      mainBkg: token('--surface'),
      primaryColor: token('--surface'),
      primaryTextColor: token('--fg'),
      primaryBorderColor: token('--muted'),
      secondaryColor: token('--bg'),
      tertiaryColor: token('--bg'),
      lineColor: token('--muted'),
      textColor: token('--fg'),
      noteBkgColor: token('--bg'),
      noteTextColor: token('--fg'),
      noteBorderColor: token('--rule'),
      actorBkg: token('--surface'),
      actorBorder: token('--muted'),
      actorTextColor: token('--fg'),
      signalColor: token('--fg'),
      signalTextColor: token('--fg'),
      labelBoxBkgColor: token('--surface'),
      labelBoxBorderColor: token('--rule'),
      fontFamily,
      fontSize: '14px',
    },
    class: natural,
    sequence: { ...natural, mirrorActors: false },
    state: natural,
    flowchart: natural,
  };
}

/**
 * Renders a diagram to SVG text (sanitised by Mermaid's strict mode). Rejects with Mermaid's message when the source
 * has an error; the caller then shows the source instead.
 */
export function renderMermaid(id: string, source: string): Promise<string> {
  const run = queue.then(async () => {
    const engine = await load();
    engine.initialize(config());
    try {
      return (await engine.render(id, source)).svg;
    } finally {
      // On an error Mermaid leaves its scratch element in the page.
      document.getElementById(id)?.remove();
      document.getElementById(`d${id}`)?.remove();
    }
  });
  queue = run.catch(() => {});
  return run;
}

export function currentColorScheme(): ColorScheme {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/**
 * Mermaid fits a diagram with `width="100%"` and an inline `max-width: <natural>px`. The viewer wants the natural
 * size, so it uses that recorded width as the real width.
 */
export function atNaturalSize(svg: string): string {
  const natural = /max-width:\s*([\d.]+)px/.exec(svg)?.[1];
  if (!natural) return svg;
  return svg
    .replace(/width="100%"/, `width="${natural}"`)
    .replace(/max-width:\s*[\d.]+px;?/, 'max-width: none;');
}
