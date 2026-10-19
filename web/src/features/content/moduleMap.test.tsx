import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import type { LessonSummary, ModuleMap as Map } from '../../data/types';
import { relationsOf } from './mapRelations';
import { ModuleMap } from './ModuleMap';
import { ModuleMnemonics } from './ModuleMnemonics';

const lessons: LessonSummary[] = ['srp', 'ocp', 'lsp', 'dip'].map((slug, index) => ({
  id: slug,
  moduleSlug: 'oop-solid',
  slug,
  title: slug.toUpperCase(),
  summary: `About ${slug}.`,
  order: index + 1,
  readingMinutes: 20,
  sectionCount: 4,
  tags: [],
}));

const map: Map = {
  groups: [{ title: 'SOLID', lessons: ['srp', 'ocp', 'lsp'] }, { title: '', lessons: ['dip'] }],
  links: [
    { from: 'ocp', to: 'lsp', label: 'needs' },
    { from: 'lsp', to: 'dip', label: null },
  ],
};

async function show() {
  await act(async () => {
    render(
      <MemoryRouter>
        <button type="button">outside</button>
        <ModuleMap map={map} lessons={lessons} />
      </MemoryRouter>,
    );
  });
}

/** The number is decoration (aria-hidden): a chip is named by its lesson title. */
const chip = (title: string) => screen.getByRole('button', { name: title });

describe('ModuleMnemonics', () => {
  it('spells the word down the side, one lesson per letter, each with its icon and hook', async () => {
    const withAids = lessons.map((lesson) => ({ ...lesson, icon: '🎯', hook: `${lesson.slug} hook` }));
    await act(async () => {
      render(
        <MemoryRouter>
          <ModuleMnemonics mnemonics={[{ word: 'S-O', lessons: ['srp', 'ocp'] }]} lessons={withAids} />
        </MemoryRouter>,
      );
    });

    const figure = screen.getByRole('figure', { name: /S-O/ });
    const rows = within(figure).getAllByRole('link');
    expect(rows.map((row) => row.textContent)).toEqual(['S🎯SRPsrp hook', 'O🎯OCPocp hook']);
    expect(rows[1]).toHaveAttribute('href', '/learn/oop-solid/ocp');
  });
});

describe('relationsOf', () => {
  it('lists outgoing links first, then incoming, with a default wording', () => {
    expect(relationsOf(map, 'lsp')).toEqual([
      { slug: 'dip', direction: 'out', label: 'leads to' },
      { slug: 'ocp', direction: 'in', label: 'needs' },
    ]);
  });
});

describe('ModuleMap', () => {
  it('draws each group as a package with its lessons, numbered in reading order', async () => {
    await show();

    expect(screen.getAllByText('«package»')).toHaveLength(2);
    expect(screen.getByText('More')).toBeInTheDocument();
    expect(chip('LSP')).toHaveTextContent('03LSP');
  });

  it('traces a lesson: it and its links stay, a card shows what it connects to', async () => {
    await show();

    fireEvent.click(chip('LSP'));

    expect(chip('LSP')).toHaveAttribute('aria-pressed', 'true');
    expect(chip('DIP')).toHaveAttribute('data-state', 'related');
    expect(chip('OCP')).toHaveAttribute('data-state', 'related');
    expect(chip('SRP')).not.toHaveAttribute('data-state');
    const card = screen.getByRole('region', { name: 'LSP' });
    expect(within(card).getByText('About lsp.')).toBeInTheDocument();
    expect(within(card).getByRole('link', { name: 'Open lesson →' })).toHaveAttribute('href', '/learn/oop-solid/lsp');
    expect(within(card).getAllByRole('button').map((b) => b.textContent)).toEqual([
      'leads to DIP →',
      '← OCP needs this',
    ]);
  });

  it('walks the map from the card', async () => {
    await show();
    fireEvent.click(chip('LSP'));

    fireEvent.click(screen.getByRole('button', { name: /OCP needs this/ }));

    expect(chip('OCP')).toHaveAttribute('aria-pressed', 'true');
    expect(chip('OCP')).toHaveFocus();
    expect(screen.getByRole('region', { name: 'OCP' })).toBeInTheDocument();
  });

  it('clears on a second tap, on Escape and on a tap outside', async () => {
    await show();

    fireEvent.click(chip('LSP'));
    fireEvent.click(chip('LSP'));
    expect(screen.queryByRole('region', { name: 'LSP' })).not.toBeInTheDocument();

    fireEvent.click(chip('LSP'));
    fireEvent.keyDown(chip('LSP'), { key: 'Escape' });
    expect(screen.queryByRole('region', { name: 'LSP' })).not.toBeInTheDocument();

    fireEvent.click(chip('LSP'));
    fireEvent.pointerDown(screen.getByRole('button', { name: 'outside' }));
    expect(screen.queryByRole('region', { name: 'LSP' })).not.toBeInTheDocument();
  });
});
