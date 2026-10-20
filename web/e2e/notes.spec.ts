/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Locator, type Page } from '@playwright/test';
import { routeContent } from './content';

// Spec 007: notes and bookmarks end to end in browser mode, signed out (the guest's notes live in localStorage).

const SRP = '/#/learn/oop-solid/srp';

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

async function addSectionNote(page: Page, section: string, text: string) {
  const actions = page.getByRole('button', { name: `Note or bookmark: ${section}` });
  await actions.scrollIntoViewIfNeeded();
  await actions.click();
  await page.getByRole('button', { name: 'Add note here' }).click();
  await page.getByRole('textbox', { name: `Note on ${section}` }).fill(text);
  await expect(page.getByText('Saved', { exact: true })).toBeVisible();
}

/** WCAG contrast between an element's text colour and the page's surface. */
async function contrastOnSurface(locator: Locator): Promise<number> {
  return locator.evaluate((element: HTMLElement) => {
    const channels = (value: string) => (value.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
    const luminance = (value: string) => {
      const [r, g, b] = channels(value).map((c) => {
        const s = c / 255;
        return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    };
    const surface = getComputedStyle(document.documentElement).getPropertyValue('--surface').trim();
    const probe = document.createElement('span');
    probe.style.color = surface;
    document.body.append(probe);
    const surfaceRgb = getComputedStyle(probe).color;
    probe.remove();
    const [light, dark] = [luminance(getComputedStyle(element).color), luminance(surfaceRgb)].sort((a, b) => b - a);
    return (light + 0.05) / (dark + 0.05);
  });
}

test('A1, A3: a section note saves as you type, marks the heading and the outline, and survives a reload', async ({ page, isMobile }) => {
  await page.goto(SRP);
  await page.getByRole('heading', { level: 1 }).waitFor();

  await addSectionNote(page, 'See it', 'Split by who asks for the change #weak');
  await page.keyboard.press('Escape');
  await page.reload();

  const heading = page.getByRole('heading', { level: 2, name: /See it/ });
  await expect(heading.getByRole('button', { name: '1 note on this section' })).toBeVisible();
  if (isMobile) await page.getByRole('button', { name: 'On this page' }).click();
  const outline = page.getByRole(isMobile ? 'dialog' : 'navigation', { name: /on this page/i });
  await expect(outline.getByRole('img', { name: '1 note' })).toBeVisible();
  if (isMobile) await page.keyboard.press('Escape');

  await heading.getByRole('button', { name: '1 note on this section' }).click();
  await expect(page.getByRole('region', { name: 'See it' }).getByRole('button', { name: /Split by who asks/ })).toBeVisible();
});

test('A8: deleting a note can be undone; a note emptied and closed is gone', async ({ page }) => {
  await page.goto(SRP);
  await page.getByRole('heading', { level: 1 }).waitFor();
  await addSectionNote(page, 'The rule', 'Keep me');

  await page.getByRole('button', { name: 'Delete' }).click();
  await page.getByRole('button', { name: 'Undo' }).click();
  await expect(page.getByRole('button', { name: /Keep me/ })).toBeVisible();

  await page.getByRole('button', { name: /Keep me/ }).click();
  await page.getByRole('textbox', { name: 'Note on The rule' }).fill('');
  // Past the autosave pause: an empty text is never saved, and closing still deletes the note.
  await page.waitForTimeout(1000);
  await page.getByRole('button', { name: 'All notes' }).click();
  await expect(page.getByText(/No notes on this lesson yet/)).toBeVisible();
});

test('A4: lessons, sections and cheat sheets can be bookmarked; Bookmarks opens each in its place', async ({ page }) => {
  await page.goto(SRP);
  await page.getByRole('heading', { level: 1 }).waitFor();
  await page.getByRole('button', { name: 'Bookmark lesson' }).click();
  const actions = page.getByRole('button', { name: 'Note or bookmark: See it' });
  await actions.scrollIntoViewIfNeeded();
  await actions.click();
  await page.getByRole('button', { name: 'Bookmark section' }).click();
  await page.goto('/#/revise/oop-solid/srp');
  await page.getByRole('button', { name: 'Bookmark cheat sheet' }).click();

  await page.goto('/#/notes');
  await page.getByRole('tab', { name: 'Bookmarks' }).click();
  const rows = page.locator('.bookmark-row');
  await expect(rows).toHaveCount(3);

  await rows.nth(1).getByRole('link').click();
  await expect(page).toHaveURL(/#\/learn\/oop-solid\/srp\?at=see-it$/);
  await page.goBack();
  await rows.nth(2).getByRole('link').click();
  await expect(page).toHaveURL(/#\/revise\/oop-solid\/srp$/);
});

test('A5, A9, A10: the Notes tab filters by tag, Revise shows the notes, and export downloads one file', async ({ page, isMobile }) => {
  await page.goto(SRP);
  await page.getByRole('heading', { level: 1 }).waitFor();
  await addSectionNote(page, 'The rule', 'One boss per class #weak');
  await page.getByRole('button', { name: 'All notes' }).click();
  await page.getByRole('button', { name: 'New note' }).click();
  await page.getByRole('textbox').fill('Ask about reasons to change #ask-in-interview');
  await expect(page.getByText('Saved', { exact: true })).toBeVisible();
  await page.keyboard.press('Escape');

  await page.goto('/#/notes');
  await page.getByRole('button', { name: /#ask-in-interview/ }).click();
  await expect(page.locator('.note-card')).toHaveCount(1);
  await page.getByRole('button', { name: /#ask-in-interview/ }).click();

  if (!isMobile) {
    const download = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Export notes' }).click();
    const file = await download;
    expect(file.suggestedFilename()).toMatch(/^lld-notes-\d{4}-\d{2}-\d{2}\.md$/);
  }

  await page.goto('/#/revise/oop-solid/srp');
  await page.getByRole('button', { name: /Your notes \(2\)/ }).click();
  await expect(page.getByText('One boss per class #weak')).toBeVisible();
});

for (const colorScheme of ['light', 'dark'] as const) {
  test(`A15: note marks are AA and labelled, with no sideways scroll at 320 px (${colorScheme})`, async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await page.emulateMedia({ colorScheme });
    await page.goto(SRP);
    await page.getByRole('heading', { level: 1 }).waitFor();
    await addSectionNote(page, 'The rule', 'A tag that is thirty letters #averyveryverylongtagnamethirty');
    await page.keyboard.press('Escape');

    const chip = page.getByRole('button', { name: '1 note on this section' });
    expect(await contrastOnSurface(chip)).toBeGreaterThanOrEqual(4.5);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);

    await page.goto('/#/notes');
    await page.locator('.note-card').waitFor();
    expect(await contrastOnSurface(page.locator('.note-tag').first())).toBeGreaterThanOrEqual(4.5);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  });
}
