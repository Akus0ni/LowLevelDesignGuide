// Renders the UML Mermaid fixtures (spec 008) with the real Mermaid in headless Chromium, so the interactive diagram
// code is tested against real markup. Run: node scripts/render-mermaid-fixtures.mjs (after npx playwright install).
// Styles are stripped; the header says where each one comes from. Regenerate when Mermaid changes its markup.
import { readFile, writeFile } from 'node:fs/promises';
import { chromium } from '@playwright/test';

const out = new URL('../src/features/content/markdown/__fixtures__/', import.meta.url);
const mermaidBundle = new URL('../node_modules/mermaid/dist/mermaid.min.js', import.meta.url);

// The line the reader adds to `mermaid steps` flowcharts (activity.ts), so `:::fork` / `:::join` become node classes;
// CSS then draws them as bars.
const ACTIVITY_CLASSES = '\n  classDef fork,join stroke-width:0px';

const fixtures = {
  'uml-class-arrows.svg': `classDiagram
  IEntity <|.. Order
  Order *-- OrderLine
  Order --> Customer
  Order o-- Discount
  Order ..> TaxPolicy
  Customer <|-- VipCustomer`,
  'uml-state.svg': `stateDiagram-v2
  [*] --> Draft
  Draft --> InReview : submit
  InReview --> Draft : reject
  InReview --> Published : approve
  Published --> [*]`,
  'uml-activity.svg': `flowchart TD
  start((" ")) --> edit["Edit a note"]
  edit --> online{"Online?"}
  online -->|yes| fork1[" "]:::fork
  online -->|no| queue["Queue the change"]
  queue --> stop(((" ")))
  fork1 --> save["Save on device"]
  fork1 --> send["Send to server"]
  save --> join1[" "]:::join
  send --> join1
  join1 --> stop${ACTIVITY_CLASSES}`,
};

const { version } = JSON.parse(await readFile(new URL('../node_modules/mermaid/package.json', import.meta.url), 'utf8'));
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  await page.setContent('<!doctype html><html><body></body></html>');
  await page.addScriptTag({ path: mermaidBundle.pathname.replace(/^\/([A-Za-z]:)/, '$1') });
  for (const [file, source] of Object.entries(fixtures)) {
    const svg = await page.evaluate(async ({ id, source }) => {
      const mermaid = globalThis.mermaid;
      mermaid.initialize({
        startOnLoad: false,
        securityLevel: 'strict',
        theme: 'base',
        class: { useMaxWidth: true },
        state: { useMaxWidth: true },
        flowchart: { useMaxWidth: true },
      });
      return (await mermaid.render(id, source)).svg;
    }, { id: `mermaid-${file.replace(/\W/g, '')}`, source });
    const stripped = svg.replace(/<style>[\s\S]*?<\/style>/g, '');
    const header = `<!-- Rendered by Mermaid ${version} from scripts/render-mermaid-fixtures.mjs (styles stripped). -->\n`;
    await writeFile(new URL(file, out), `${header}${stripped}\n`);
    console.log(`wrote ${file}`);
  }
} finally {
  await browser.close();
}
