// Renders every ```mermaid block under content/ (or the folders given) with the real Mermaid in headless Chromium and
// lists the ones that fail to draw, with file and line. The content validator checks structure; only Mermaid itself
// can say whether a diagram parses. Run: node scripts/check-mermaid.mjs [content/uml ...]
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const repo = fileURLToPath(new URL('../..', import.meta.url));
const roots = process.argv.slice(2).length > 0 ? process.argv.slice(2) : ['content'];
const bundle = fileURLToPath(new URL('../node_modules/mermaid/dist/mermaid.min.js', import.meta.url));
// The line the reader adds to `mermaid steps` flowcharts (umlSource.ts ACTIVITY_CLASS_DEFS).
const ACTIVITY_CLASS_DEFS = '\n  classDef fork,join stroke-width:0px';

function markdownFiles(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) return markdownFiles(path);
    return path.endsWith('.md') ? [path] : [];
  });
}

function diagramsIn(file) {
  const lines = readFileSync(file, 'utf8').split(/\r?\n/);
  const found = [];
  for (let i = 0; i < lines.length; i++) {
    const open = /^(\s*)```mermaid\b(.*)$/.exec(lines[i]);
    if (!open) continue;
    const indent = open[1].length;
    const body = [];
    let j = i + 1;
    while (j < lines.length && !/^\s*```\s*$/.test(lines[j])) body.push(lines[j++].slice(indent));
    const source = body.join('\n');
    found.push({ file, line: i + 1, source: /\bsteps\b/.test(open[2]) ? source + ACTIVITY_CLASS_DEFS : source });
    i = j;
  }
  return found;
}

const diagrams = roots.flatMap((root) => markdownFiles(join(repo, root))).flatMap(diagramsIn);
const browser = await chromium.launch();
let failures = 0;
try {
  const page = await browser.newPage();
  await page.setContent('<!doctype html><html><body></body></html>');
  await page.addScriptTag({ path: bundle });
  for (const [index, diagram] of diagrams.entries()) {
    const error = await page.evaluate(
      async ({ id, source }) => {
        const mermaid = globalThis.mermaid;
        mermaid.initialize({ startOnLoad: false, securityLevel: 'strict', theme: 'base' });
        try {
          await mermaid.render(id, source);
          return null;
        } catch (e) {
          document.getElementById(id)?.remove();
          document.getElementById(`d${id}`)?.remove();
          return String(e?.message ?? e).split('\n').slice(0, 3).join(' ');
        }
      },
      { id: `check-${index}`, source: diagram.source },
    );
    if (error) {
      failures++;
      console.log(`${relative(repo, diagram.file)}(${diagram.line}): ${error}`);
    }
  }
} finally {
  await browser.close();
}
console.log(`mermaid: ${diagrams.length - failures} of ${diagrams.length} diagrams draw`);
process.exitCode = failures > 0 ? 1 : 0;
