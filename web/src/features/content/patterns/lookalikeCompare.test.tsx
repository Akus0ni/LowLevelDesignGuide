import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { routes } from '../../../app/routes';
import type { LessonDetail, LessonSummary, ModuleDetail } from '../../../data/types';
import { AppProviders } from '../../../test/AppProviders';
import { fakeDataSource } from '../../../test/fakeDataSource';
import { patternFacts } from './patternFacts';

vi.mock('../markdown/mermaid', async (original) => ({
  ...(await original<typeof import('../markdown/mermaid')>()),
  renderMermaid: vi.fn(() => Promise.resolve('<svg width="100%" style="max-width: 320px;"><text>structure</text></svg>')),
}));

const bodyOf = (title: string) =>
  [
    `# ${title}`,
    '',
    '> [!REMEMBER]',
    `> ${title} in one line.`,
    '',
    '## The idea',
    '',
    `${title} does **one** thing.`,
    '',
    'Second paragraph.',
    '',
    '## Structure',
    '',
    '```mermaid',
    'classDiagram',
    `  Client --> ${title.replace(' ', '')}`,
    '```',
    '',
    '## Seen in .NET',
    '',
    '- `Something`',
    '',
    '## Travels with',
    '',
    '- Others.',
  ].join('\n');

const summaries: LessonSummary[] = [
  ['factory-method', 'Factory Method', 'Fm'],
  ['abstract-factory', 'Abstract Factory', 'Af'],
  ['builder', 'Builder', 'Bu'],
].map(([slug, title, symbol], index) => ({
  id: slug,
  moduleSlug: 'gof-patterns',
  slug,
  title,
  summary: `${title} summary.`,
  order: index + 1,
  readingMinutes: 3,
  sectionCount: 4,
  tags: [],
  hook: `${title} hook`,
  symbol,
}));

const module: ModuleDetail = {
  slug: 'gof-patterns',
  title: 'GoF Design Patterns',
  summary: 'Patterns.',
  lessons: summaries,
  lookalikes: [{ lessons: ['factory-method', 'abstract-factory'], difference: 'One product or a family.', ask: 'One thing or a set?' }],
};

const lessonOf = (summary: LessonSummary): LessonDetail => ({
  ...summary,
  moduleTitle: module.title,
  bodyMarkdown: bodyOf(summary.title),
  outline: [],
  cheatSheetTitle: `Cheat sheet: ${summary.title}`,
  previous: null,
  next: null,
  terms: [],
});

async function visit(path: string) {
  const source = fakeDataSource({
    getModule: vi.fn().mockResolvedValue(module),
    getLesson: vi.fn((id: string) => Promise.resolve(lessonOf(summaries.find((s) => s.id === id)!))),
  });
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  await act(async () => {
    render(
      <AppProviders source={source}>
        <RouterProvider router={router} />
      </AppProviders>,
    );
  });
  return router;
}

describe('patternFacts', () => {
  it('takes the first paragraph of the idea and the first diagram of the structure', () => {
    expect(patternFacts(bodyOf('Builder'))).toEqual({
      idea: 'Builder does **one** thing.',
      structure: 'classDiagram\n  Client --> Builder',
    });
    expect(patternFacts('# Nothing\n\nText.')).toEqual({ idea: '', structure: null });
  });
});

describe('look-alike compare', () => {
  it('shows the difference, the question and each pattern with its idea and structure', async () => {
    await visit('/learn/gof-patterns/compare/factory-method-vs-abstract-factory');

    expect(await screen.findByRole('heading', { level: 1, name: 'Factory Method vs Abstract Factory' })).toBeInTheDocument();
    const stamp = screen.getByRole('complementary', { name: 'The difference' });
    expect(stamp).toHaveTextContent('One product or a family.');
    expect(stamp).toHaveTextContent('Ask yourself: One thing or a set?');
    expect(await screen.findByText(/Factory Method does/)).toBeInTheDocument();
    expect(screen.getByText(/Abstract Factory does/)).toBeInTheDocument();
    expect(screen.queryByText('Second paragraph.')).not.toBeInTheDocument();
  });

  it('switches between patterns on a narrow screen, with the arrow keys too', async () => {
    await visit('/learn/gof-patterns/compare/factory-method-vs-abstract-factory');

    const tabs = await screen.findAllByRole('tab');
    expect(tabs.map((tab) => tab.textContent)).toEqual(['Fm Factory Method', 'Af Abstract Factory']);
    expect(tabs[0]).toHaveAttribute('aria-selected', 'true');
    fireEvent.keyDown(tabs[0], { key: 'ArrowRight' });
    expect(screen.getByRole('tab', { name: /Abstract Factory/ })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel', { name: /Abstract Factory/ })).toBeInTheDocument();
  });

  it('says so when the comparison does not exist', async () => {
    await visit('/learn/gof-patterns/compare/builder-vs-singleton');

    expect(await screen.findByText(/This comparison doesn't exist/)).toBeInTheDocument();
  });

  it('links a lesson to its compares after the Seen in .NET section', async () => {
    await visit('/learn/gof-patterns/abstract-factory');

    const strip = await screen.findByRole('navigation', { name: 'Often confused with' });
    expect(within(strip).getByRole('link', { name: /Factory Method ⇄ Abstract Factory/ })).toHaveAttribute(
      'href',
      '/learn/gof-patterns/compare/factory-method-vs-abstract-factory',
    );
  });

  it('shows no strip on a lesson without look-alikes', async () => {
    await visit('/learn/gof-patterns/builder');

    expect(await screen.findByText(/Builder does/)).toBeInTheDocument();
    expect(screen.queryByRole('navigation', { name: 'Often confused with' })).not.toBeInTheDocument();
  });
});
