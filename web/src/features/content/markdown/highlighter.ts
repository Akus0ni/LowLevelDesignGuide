import type { HighlighterCore, ThemedToken } from 'shiki/core';

/**
 * Syntax highlighting with Shiki, loaded on first use: the core, the JavaScript regex engine (no WASM), two themes and
 * only the languages lessons use. Light and dark colours come back together as CSS variables, so a theme change needs
 * no re-highlight.
 */
const LANGUAGES = {
  csharp: () => import('shiki/langs/csharp.mjs'),
  typescript: () => import('shiki/langs/typescript.mjs'),
  tsx: () => import('shiki/langs/tsx.mjs'),
  json: () => import('shiki/langs/json.mjs'),
  shellscript: () => import('shiki/langs/shellscript.mjs'),
  powershell: () => import('shiki/langs/powershell.mjs'),
  xml: () => import('shiki/langs/xml.mjs'),
  yaml: () => import('shiki/langs/yaml.mjs'),
  sql: () => import('shiki/langs/sql.mjs'),
} as const;

export type Language = keyof typeof LANGUAGES;

const ALIASES: Record<string, Language> = {
  csharp: 'csharp',
  cs: 'csharp',
  'c#': 'csharp',
  typescript: 'typescript',
  ts: 'typescript',
  tsx: 'tsx',
  json: 'json',
  bash: 'shellscript',
  sh: 'shellscript',
  shell: 'shellscript',
  powershell: 'powershell',
  ps1: 'powershell',
  pwsh: 'powershell',
  xml: 'xml',
  html: 'xml',
  csproj: 'xml',
  yaml: 'yaml',
  yml: 'yaml',
  sql: 'sql',
};

/** The Shiki language for a code fence's info string, or null for plain text (unknown or no language). */
export function languageOf(info: string | undefined): Language | null {
  return info ? (ALIASES[info.toLowerCase()] ?? null) : null;
}

let highlighter: Promise<HighlighterCore> | null = null;

function load(): Promise<HighlighterCore> {
  highlighter ??= (async () => {
    const [{ createHighlighterCore }, { createJavaScriptRegexEngine }] = await Promise.all([
      import('shiki/core'),
      import('shiki/engine/javascript'),
    ]);
    return createHighlighterCore({
      themes: [import('shiki/themes/github-light.mjs'), import('shiki/themes/github-dark.mjs')],
      langs: Object.values(LANGUAGES).map((language) => language()),
      engine: createJavaScriptRegexEngine(),
    });
  })();
  return highlighter;
}

/** Lines of tokens; each token's `htmlStyle` carries `--shiki-light` / `--shiki-dark`. */
export async function highlight(code: string, language: Language): Promise<ThemedToken[][]> {
  const core = await load();
  return core.codeToTokens(code, {
    lang: language,
    themes: { light: 'github-light', dark: 'github-dark' },
    defaultColor: false,
  }).tokens;
}

/** Start loading when the browser is idle, so the first lesson with code doesn't wait for it. */
export function warmUpHighlighter(): void {
  const start = () => void load().catch(() => {});
  if ('requestIdleCallback' in window) window.requestIdleCallback(start, { timeout: 2000 });
  else setTimeout(start, 1);
}
