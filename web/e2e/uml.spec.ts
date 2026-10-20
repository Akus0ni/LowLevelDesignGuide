/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Locator, type Page } from '@playwright/test';
import { allModules, lessonSlugsOf, routeContent } from './content';

// Spec 008 acceptance criteria A1, A5–A10 against the repository's real UML content.

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

const UML = '/#/learn/uml';
const lesson = (slug: string) => `${UML}/${slug}`;

async function expectNoSidewaysScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, 'the page must never scroll sideways').toBeLessThanOrEqual(0);
}

/** The diagram figure (inline, not the pair's outer box) whose strip has the given button. */
function diagramWith(page: Page, button: string | RegExp): Locator {
  return page.locator('figure.diagram').filter({ has: page.getByRole('button', { name: button, exact: true }) });
}

let pins = 0;

/**
 * Marks the element a locator finds now, so later steps keep acting on it even when the strip's buttons change (a
 * filter by "has a run button" would move on to the next diagram once Run is pressed).
 */
async function pin(page: Page, locator: Locator): Promise<Locator> {
  const id = `pin-${++pins}`;
  await locator.evaluate((element, value) => element.setAttribute('data-e2e', value), id);
  return page.locator(`[data-e2e="${id}"]`);
}

test('A1: UML has 10 lessons in reading order, at module position 4', async ({ page }) => {
  expect(allModules().map((m) => m.slug)).toEqual([
    'start',
    'oop-solid',
    'gof-patterns',
    'enterprise-patterns',
    'uml',
    'case-studies',
  ]);
  expect(lessonSlugsOf('uml')).toEqual([
    'class-basics',
    'class-relationships',
    'multiplicity',
    'object-diagram',
    'component-deployment',
    'use-case-diagram',
    'sequence-diagram',
    'state-diagram',
    'activity-diagram',
    'uml-in-interviews',
  ]);

  await page.goto(UML);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('UML');
  await expect(page.getByRole('region', { name: 'Lessons' }).getByRole('listitem')).toHaveCount(10);
  await expectNoSidewaysScroll(page);
});

test('A5: Code ↔ UML lights up the code behind an arrow, and the arrow behind a code line', async ({ page }) => {
  await page.goto(lesson('class-relationships'));
  const pair = page.getByRole('figure', { name: 'Code and UML' });
  await pair.scrollIntoViewIfNeeded();
  const arrow = pair.locator('.edge-hit[data-edge-key="Order*--OrderLine"]');
  await expect(arrow).toHaveAttribute('aria-label', 'Order is made of OrderLine');

  await arrow.focus();
  await page.keyboard.press('Enter');
  const lit = pair.locator('.uml-pair-code .code-line[data-mark]');
  await expect(lit.first()).toContainText('List<OrderLine> _lines');

  // Tapping a class's code line selects that class in the diagram.
  const code = pair.locator('.uml-pair-code');
  if (await pair.getByRole('tab', { name: 'code' }).isVisible()) await pair.getByRole('tab', { name: 'code' }).click();
  await code.locator('.code-line', { hasText: 'public sealed class Customer' }).click();
  await expect(pair.locator('g.node[id*="classId-Customer-"]')).toHaveAttribute('aria-pressed', 'true');
  await expectNoSidewaysScroll(page);
});

test('A5: on a phone the pair is a Diagram / Code switch that never changes the box height', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'phones only');
  await page.goto(lesson('class-relationships'));
  const pair = page.getByRole('figure', { name: 'Code and UML' });
  await pair.scrollIntoViewIfNeeded();
  await expect(pair).toHaveAttribute('data-layout', 'tabs');
  const before = (await pair.boundingBox())!.height;
  await pair.getByRole('tab', { name: 'code' }).click();
  await expect(pair.getByRole('tab', { name: 'code' })).toHaveAttribute('aria-selected', 'true');
  expect(Math.abs((await pair.boundingBox())!.height - before)).toBeLessThan(2);
});

test('A6: the arrow legend opens from a class diagram and closes with Escape', async ({ page }) => {
  await page.goto(lesson('class-basics'));
  const diagram = diagramWith(page, 'What the arrows mean').first();
  await diagram.scrollIntoViewIfNeeded();
  await diagram.getByRole('button', { name: 'What the arrows mean' }).click();
  const legend = page.getByRole('dialog');
  await expect(legend.getByRole('heading', { name: 'The six arrows' })).toBeVisible();
  await expect(legend.getByText('class Circle : Shape')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('A6: the legend also opens from the expanded viewer', async ({ page }) => {
  await page.goto(lesson('class-basics'));
  const diagram = diagramWith(page, 'What the arrows mean').first();
  await diagram.scrollIntoViewIfNeeded();
  await diagram.getByRole('button', { name: /^Expand/ }).click();
  const viewer = page.getByRole('dialog');
  await viewer.getByRole('button', { name: 'What the arrows mean' }).click();
  await expect(page.getByRole('heading', { name: 'The six arrows' })).toBeVisible();
});

test('A7: a state diagram runs event by event to its final state', async ({ page }) => {
  await page.goto(lesson('state-diagram'));
  const machine = page.locator('figure.uml-pair figure.diagram');
  await machine.scrollIntoViewIfNeeded();
  await machine.getByRole('button', { name: 'run', exact: true }).click();
  const events = machine.getByRole('group', { name: 'Events' });
  await expect(events.getByRole('button')).toHaveText(['ship', 'cancel']);
  await events.getByRole('button', { name: 'ship' }).click();
  await events.getByRole('button', { name: 'deliver' }).click();
  await events.getByRole('button', { name: 'finish' }).click();
  await expect(events.getByText('Final state reached.')).toBeVisible();
  await machine.getByRole('button', { name: 'stop' }).click();
  await expect(machine.getByRole('group', { name: 'Events' })).toHaveCount(0);
});

test('A7: tapping a state traces it, and the Auth case study diagram runs too', async ({ page }) => {
  await page.goto(lesson('state-diagram'));
  const readIt = await pin(page, diagramWith(page, 'run').nth(1));
  await readIt.scrollIntoViewIfNeeded();
  await readIt.locator('g.node[id*="-state-Pending-"]').click();
  await expect(readIt.getByText(/Pending · 3 links/)).toBeVisible();

  await page.goto('/#/learn/case-studies/auth');
  const auth = await pin(page, diagramWith(page, 'run').first());
  await auth.scrollIntoViewIfNeeded();
  await auth.getByRole('button', { name: 'run', exact: true }).click();
  await expect(auth.getByRole('group', { name: 'Events' }).getByRole('button', { name: /Approve/ })).toBeVisible();
});

test('A8: an activity diagram steps through its decision, fork and join', async ({ page }) => {
  await page.goto(lesson('activity-diagram'));
  const flow = await pin(page, diagramWith(page, 'step through').nth(2)); // From requirements: the cash machine
  await flow.scrollIntoViewIfNeeded();
  await flow.getByRole('button', { name: 'step through' }).click();
  const next = flow.getByRole('group', { name: 'Next step' });
  await next.getByRole('button', { name: 'next' }).click(); // → enter PIN
  await next.getByRole('button', { name: 'next' }).click(); // → PIN right?
  await expect(next.getByRole('button')).toHaveText(['no', 'yes']);
  await next.getByRole('button', { name: 'yes' }).click(); // → choose an amount
  await next.getByRole('button', { name: 'next' }).click(); // → enough money?
  await next.getByRole('button', { name: 'yes' }).click(); // → take it from the account
  await next.getByRole('button', { name: 'next' }).click(); // → the fork
  await next.getByRole('button', { name: 'next' }).click(); // → both branches
  await expect(flow.locator('g.node[data-current]')).toHaveCount(2);
  await next.getByRole('button', { name: 'next' }).click(); // → join
  await next.getByRole('button', { name: 'next' }).click(); // → end
  await expect(next.getByText('The flow ends here.')).toBeVisible();
  await flow.getByRole('button', { name: 'Previous step' }).click();
  await expect(next.getByRole('button', { name: 'next' })).toBeVisible();
});

test('A8: plain flowcharts stay static', async ({ page }) => {
  await page.goto(lesson('class-basics'));
  await expect(page.getByRole('button', { name: 'step through' })).toHaveCount(0);
});

test('A9: "Which diagram?" ends at a lesson; the GoF picker keeps its title', async ({ page }) => {
  await page.goto(UML);
  await page.getByRole('button', { name: 'Which diagram?' }).click();
  const sheet = page.getByRole('dialog', { name: 'Which diagram?' });
  await sheet.getByRole('button', { name: 'What happens over time' }).click();
  await sheet.getByRole('button', { name: "One object's life, event by event" }).click();
  await sheet.getByRole('link', { name: /State diagram/ }).click();
  await expect(page).toHaveURL(/#\/learn\/uml\/state-diagram$/);

  await page.goto('/#/learn/gof-patterns');
  await expect(page.getByRole('button', { name: 'Which pattern?' })).toBeVisible();
});

test('A10: the Mistakes stamp and both phrase mnemonics', async ({ page }) => {
  await page.goto(UML);
  const structure = page.getByRole('figure', { name: /Clever Robots/ });
  await expect(structure.locator('figcaption b')).toHaveText(['C', 'R', 'M', 'O', 'C']);
  await expect(page.getByRole('figure', { name: /Users Always/ }).getByRole('link')).toHaveCount(4);

  await page.goto(lesson('class-basics'));
  const stamp = page.getByRole('complementary', { name: 'common mistakes' });
  await expect(stamp).toBeVisible();
  // Drawn as the Overuse stamp: a 2px frame in the warning tone.
  expect(await stamp.evaluate((element) => getComputedStyle(element).borderTopWidth)).toBe('2px');
});
